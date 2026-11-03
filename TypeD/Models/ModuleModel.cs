using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class ModuleModel : IModuleModel
    {
        // Models
        public IHookModel HookModel { get; set; }
        public IResourceModel ResourceModel { get; set; }
        public ISaveModel SaveModel { get; set; }
        public ITyperModel TyperModel { get; set; }
        public ILogModel LogModel { get; set; }

        // Paths
        public static string ModuleCachePath { get { return $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/TypeO/ModulesCache"; } }

        // Constructors
        public ModuleModel() { }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            HookModel = ResourceModel.Get<IHookModel>();
            SaveModel = ResourceModel.Get<ISaveModel>();
            TyperModel = ResourceModel.Get<ITyperModel>();
            LogModel = ResourceModel.Get<ILogModel>();
        }

        // Functions
        public async Task Initialize(Project project)
        {
            var typeOPath = Path.Combine(project.Location, "typeo.json");
            if (File.Exists(typeOPath)) return;

            var typeO = new
            {
                name = project.ProjectName,
                version = "0.1.0",
                type = "exe",
                dependencies = new string[0],
                project = new[] { new { Name = project.ProjectName, type = "" } }
            };
            await File.WriteAllTextAsync(typeOPath, JsonSerializer.Serialize(typeO, new JsonSerializerOptions { WriteIndented = true }));
        }

        public async Task<bool> Resolve(Project project, Action<ProgressUpdate> report = null)
        {
            return await Resolve(project, false, report);
        }

        private async Task<bool> Resolve(Project project, bool force, Action<ProgressUpdate> report = null)
        {
            void Report(int progress, string message, string subTask = null, int? subProgress = null)
            {
                report?.Invoke(new ProgressUpdate(progress, message, subTask, subProgress));
            }

            Report(0, "Checking module configuration");
            await Initialize(project);

            // Nothing that typer or the build depends on has changed since the last resolve, so reuse that result
            var cache = force ? null : ReadCache(project);
            var cachedModules = cache?.Modules?.Select(m => new Module() { Name = m.Name, Version = m.Version }).ToList();
            var resolveCurrent = cache != null && cachedModules.Count > 0 && cache.ResolveStamp == ResolveStamp(project, cachedModules);

            bool resolved;
            if (resolveCurrent)
            {
                Report(40, "Dependencies are up to date");
                SetModules(project, cachedModules);
                resolved = true;
            }
            else
            {
                Report(5, "Resolving dependencies", "Starting typer");
                resolved = await TyperModel.Resolve(project.Location, line => Report(10, "Resolving dependencies", line));
                if (resolved)
                {
                    Report(35, "Synchronizing modules", "Reading resolved dependencies");
                    await SyncModules(project);
                }
                else
                {
                    LogModel.Log("Could not resolve dependencies, using the modules already known by the project");
                }
            }

            var buildCurrent = resolveCurrent && cache.BuildStamp == BuildStamp(project) && project.Modules.All(IsBuilt);
            var moduleCount = project.Modules.Count;
            var moduleIndex = 0;
            foreach (var module in project.Modules)
            {
                var moduleStart = 40 + 55 * moduleIndex / Math.Max(moduleCount, 1);
                var step = $"Module {moduleIndex + 1}/{moduleCount}: {module.Name}";
                if (!buildCurrent)
                {
                    // Local modules are under development so keep their own solution and dependencies in sync as well
                    if (resolved && !resolveCurrent && module.IsLocal && File.Exists(module.TypeOPath))
                    {
                        Report(moduleStart, "Preparing modules", $"Resolving dependencies of {module.Name}", 0);
                        await TyperModel.Resolve(module.ModulePath, line => Report(moduleStart, "Preparing modules", $"{module.Name}: {line}"));
                    }
                    Report(moduleStart, "Building modules", $"Building {module.Name} ({step})", 100 * moduleIndex / Math.Max(moduleCount, 1));
                    await Build(module);
                }
                if (module.Assembly == null)
                {
                    Report(moduleStart, "Loading modules", $"Loading {module.Name}", 100 * (moduleIndex + 1) / Math.Max(moduleCount, 1));
                    LoadAssembly(project, module);
                }
                moduleIndex++;
            }

            if (resolved)
            {
                Report(95, "Saving module cache");
                WriteCache(project, project.Modules.All(IsBuilt));
            }
            Report(100, "Modules ready");
            return resolved;
        }

        public async Task<bool> Add(Project project, string author, string name, string version)
        {
            if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(version)) return false;
            await Initialize(project);
            return await Apply(project, await TyperModel.AddDependency(project.Location, $"{author}@{name}-{version}"));
        }

        public async Task<bool> Remove(Project project, string name)
        {
            await Initialize(project);
            var dependency = (await TyperModel.ListDependencies(project.Location)).FirstOrDefault(d => d.Name == name && !d.Nuget);
            if (dependency == null) return false;

            if (dependency.Overridden)
            {
                await TyperModel.RemoveOverride(project.Location, name);
            }
            return await Apply(project, await TyperModel.RemoveDependency(project.Location, name));
        }

        public async Task<bool> Update(Project project, string name = null, string version = null)
        {
            await Initialize(project);
            return await Apply(project, await TyperModel.UpdateDependency(project.Location, name, version));
        }

        public async Task<bool> UseLocal(Project project, string name, string path)
        {
            await Initialize(project);
            return await Apply(project, await TyperModel.AddOverride(project.Location, name, path));
        }

        public async Task<bool> UseReleased(Project project, string name)
        {
            await Initialize(project);
            return await Apply(project, await TyperModel.RemoveOverride(project.Location, name));
        }

        public async Task<bool> UpdateLocal(Project project, string name)
        {
            var module = project.Modules.Find(m => m.Name == name && m.IsLocal);
            if (module == null) return false;

            return await Apply(project, await TyperModel.UpdateDependency(module.ModulePath));
        }

        public async Task<bool> CreateLocal(Project project, string name, string outputPath)
        {
            if (!await TyperModel.CreateModule(name, outputPath)) return false;

            // The new module has no release yet, so it is added as a dependency that is overridden by the local folder
            await Initialize(project);
            var localPath = Path.Combine(outputPath, name.ToLower());
            if (!await TyperModel.AddDependency(project.Location, $"Local@{name}-0.1.0")) return false;
            return await Apply(project, await TyperModel.AddOverride(project.Location, name, localPath));
        }

        public async Task<bool> Build(Module module)
        {
            if (IsBuilt(module) && (!module.IsLocal || module.Assembly != null)) return true;

            var solution = Path.Combine(module.ModulePath, $"{module.Name}.sln");
            var target = File.Exists(solution) ? solution : Path.Combine(module.ModulePath, module.Name, $"{module.Name}.csproj");
            if (!File.Exists(target)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = module.ModulePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var arg in new[] { "build", target, "-c", "Debug", "--verbosity", "quiet" }) startInfo.ArgumentList.Add(arg);

            LogModel.Log($"Building module {module.Name}");
            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                LogModel.Log(await output);
                LogModel.Log(await error);
            }

            return IsBuilt(module);
        }

        public void LoadAssembly(Project project, Module module)
        {
            if (!File.Exists(module.ModuleDLLPath)) return;

            module.Assembly = System.Reflection.Assembly.LoadFrom(module.ModuleDLLPath);
            module.ModuleTypeInfo = GetModuleType(module);
            if (module.HaveDevModule && File.Exists(module.ModuleDevDLLPath))
            {
                module.DevAssembly = System.Reflection.Assembly.LoadFrom(module.ModuleDevDLLPath);
            }
            InitializeTypeD(project, module);
        }

        public void UnloadAssembly(Module module)
        {
            if (module.Assembly == null) return;

            UninitializeTypeD(module);
            module.Assembly = null;
            module.ModuleTypeInfo = null;
        }

        // Internal functions
        private async Task<bool> Apply(Project project, bool changed)
        {
            if (!changed) return false;

            var resolved = await Resolve(project, true);
            SaveModel.AddSave<ProjectSaveContext>(project);
            ResourceModel.Get<IProjectModel>().InitAndSaveCode(project, new ProgramCode());
            return resolved;
        }

        // Replaces the project's modules with the dependencies typer resolved, including the dependencies of the modules themselves
        private async Task SyncModules(Project project)
        {
            var modules = new List<Module>();
            var visited = new HashSet<string>();

            async Task Collect(string path)
            {
                foreach (var dependency in await TyperModel.ListDependencies(path))
                {
                    if (dependency.Nuget || dependency.Tags.Count > 0 || dependency.Name == "TypeD") continue;
                    if (!visited.Add(dependency.Name)) continue;

                    // Overrides declared by a module are relative to that module, so make the path absolute
                    var version = dependency.IsLocal ? $"{Path.GetFullPath(dependency.LocalPath, path)};local" : dependency.Version;
                    var module = project.Modules.Find(m => m.Name == dependency.Name && m.Version == version)
                        ?? new Module() { Name = dependency.Name, Version = version };
                    modules.Add(module);

                    if (File.Exists(module.TypeOPath))
                    {
                        await Collect(module.ModulePath);
                    }
                }
            }
            await Collect(project.Location);

            SetModules(project, modules);
        }

        // Replaces the project's modules, keeping the already loaded instances of modules that are still used
        private void SetModules(Project project, List<Module> modules)
        {
            for (var i = 0; i < modules.Count; i++)
            {
                modules[i] = project.Modules.Find(m => m.Name == modules[i].Name && m.Version == modules[i].Version) ?? modules[i];
            }

            foreach (var removed in project.Modules.Where(m => !modules.Contains(m)))
            {
                UnloadAssembly(removed);
            }
            project.Modules.Clear();
            project.Modules.AddRange(modules);
        }

        // Cache
        private class ModuleCache
        {
            public string ResolveStamp { get; set; }
            public string BuildStamp { get; set; }
            public List<ModuleCacheEntry> Modules { get; set; } = new List<ModuleCacheEntry>();
        }

        private class ModuleCacheEntry
        {
            public string Name { get; set; }
            public string Version { get; set; }
        }

        private static string CachePath(Project project)
        {
            return Path.Combine(project.ProjectTypeOPath, "cache", "modules.json");
        }

        private ModuleCache ReadCache(Project project)
        {
            try
            {
                var path = CachePath(project);
                return File.Exists(path) ? JsonSerializer.Deserialize<ModuleCache>(File.ReadAllText(path)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void WriteCache(Project project, bool built)
        {
            try
            {
                var cache = new ModuleCache()
                {
                    ResolveStamp = ResolveStamp(project, project.Modules),
                    BuildStamp = built ? BuildStamp(project) : null,
                    Modules = project.Modules.Select(m => new ModuleCacheEntry() { Name = m.Name, Version = m.Version }).ToList()
                };
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath(project)));
                File.WriteAllText(CachePath(project), JsonSerializer.Serialize(cache));
            }
            catch (Exception e)
            {
                LogModel.Log($"Could not write the module cache: {e.Message}");
            }
        }

        // Everything the dependency resolving depends on: the product and override files of the project and its modules, and the project files typer writes to
        private static string ResolveStamp(Project project, IEnumerable<Module> modules)
        {
            var hash = new StringBuilder();
            void AddFile(string path)
            {
                hash.Append(path).Append(File.Exists(path) ? File.ReadAllText(path) : "<missing>");
            }
            void AddStamp(string path)
            {
                var info = new FileInfo(path);
                hash.Append(path).Append(info.Exists ? $"{info.Length}:{info.LastWriteTimeUtc.Ticks}" : "<missing>");
            }

            foreach (var folder in new[] { project.Location }.Concat(modules.Select(m => m.ModulePath)))
            {
                AddFile(Path.Combine(folder, "typeo.json"));
                AddFile(Path.Combine(folder, "typeo_override.json"));
            }
            foreach (var module in modules) hash.Append(module.Name).Append(module.Version);
            AddStamp(project.ProjectCSProjPath);
            AddStamp(Path.Combine(project.Location, $"{Path.GetFileNameWithoutExtension(project.CSSolutionPath)}.sln"));
            return Hash(hash.ToString());
        }

        // The source files of every local module, since those are the ones that change between loads; released modules are immutable
        private static string BuildStamp(Project project)
        {
            var hash = new StringBuilder();
            var skipped = new[] { "bin", "obj", ".git", ".vs", "typer", "node_modules" };
            var extensions = new[] { ".cs", ".csproj", ".axaml", ".xaml", ".resx", ".json", ".props", ".targets", ".sln" };
            foreach (var module in project.Modules.Where(m => m.IsLocal).OrderBy(m => m.Name))
            {
                hash.Append(module.Name);
                if (!Directory.Exists(module.ModulePath)) continue;

                var pending = new Stack<string>();
                pending.Push(module.ModulePath);
                while (pending.Count > 0)
                {
                    var folder = pending.Pop();
                    foreach (var directory in Directory.EnumerateDirectories(folder).OrderBy(d => d))
                    {
                        if (!skipped.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase)) pending.Push(directory);
                    }
                    foreach (var file in Directory.EnumerateFiles(folder).OrderBy(f => f))
                    {
                        if (!extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)) continue;
                        var info = new FileInfo(file);
                        hash.Append(file).Append(info.Length).Append(info.LastWriteTimeUtc.Ticks);
                    }
                }
            }
            return Hash(hash.ToString());
        }

        private static string Hash(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }

        private bool IsBuilt(Module module)
        {
            return File.Exists(module.ModuleDLLPath) && (!module.HaveDevModule || File.Exists(module.ModuleDevDLLPath));
        }

        private void InitializeTypeD(Project project, Module module)
        {
            if (!module.HaveDevModule || module.DevAssembly == null) return;

            var typeDInitType = module.DevAssembly.GetTypes().FirstOrDefault(t => { return t.IsSubclassOf(typeof(TypeDModuleInitializer)); });
            if (typeDInitType == null) return;

            module.TypeDModuleInitializer = Activator.CreateInstance(typeDInitType) as TypeDModuleInitializer;
            module.TypeDModuleInitializer.Hooks = HookModel;
            module.TypeDModuleInitializer.Resources = ResourceModel;
            module.TypeDModuleInitializer.Initializer(project);
        }

        private void UninitializeTypeD(Module module)
        {
            if (!module.HaveDevModule || module.DevAssembly == null) return;

            var typeDInitType = module.DevAssembly.GetTypes().FirstOrDefault(t => { return t.IsSubclassOf(typeof(TypeDModuleInitializer)); });
            if (typeDInitType == null) return;

            module.TypeDModuleInitializer?.Uninitializer();
        }

        private System.Reflection.TypeInfo GetModuleType(Module module)
        {
            if (module.Name == "TypeOCore") return null;
            System.Reflection.TypeInfo moduleType = null;
            foreach (var type in module.Assembly.DefinedTypes)
            {
                if (type?.BaseType?.FullName?.StartsWith("TypeOEngine.Typedeaf.Core.Engine.Module") == true)
                {
                    moduleType = type;
                    break;
                }
            }

            return moduleType;
        }
    }
}
