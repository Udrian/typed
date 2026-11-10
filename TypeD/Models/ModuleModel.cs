using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TypeD.Code;
using TypeD.Models.Data;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Interfaces;
using Module = TypeD.Models.Data.Module;

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
            var cachedModules = cache?.Modules?.Select(m => new Module() { Name = m.Name, Author = m.Author, Version = m.Version }).ToList();
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
                var moduleStart = 40 + 50 * moduleIndex / Math.Max(moduleCount, 1);
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
                moduleIndex++;
            }

            // Every assembly is loaded before any is initialized, since initializing resolves the modules it
            // depends on; a module initialized earlier would bind a copy from its own bin folder instead
            foreach (var module in project.Modules.Where(m => m.Assembly == null))
            {
                LoadAssemblyFiles(module);
            }
            // Modules initialize after the modules they depend on, so every module can use its dependencies while initializing
            var pending = InitializationOrder(project).Where(m => m.Assembly != null && !m.Initialized).ToList();
            for (var i = 0; i < pending.Count; i++)
            {
                Report(90, "Loading modules", $"Loading {pending[i].Name}", 100 * (i + 1) / pending.Count);
                InitializeAssembly(project, pending[i]);
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

            var target = BuildTarget(module);
            if (target == null) return false;

            LogModel.Log($"Building module {module.Name}");
            var output = new List<string>();
            if (await RunBuild(module, target, "quiet", line => { lock (output) output.Add(line); }) != 0)
            {
                lock (output) LogModel.Log(string.Join(Environment.NewLine, output));
            }

            return IsBuilt(module);
        }

        public async Task<bool> Rebuild(Module module, Action<string> output = null)
        {
            var target = BuildTarget(module);
            if (target == null)
            {
                output?.Invoke($"error: No solution or C# project was found for module {module.Name} in '{module.ModulePath}'.");
                return false;
            }

            LogModel.Log($"Rebuilding module {module.Name}");
            return await RunBuild(module, target, "minimal", output) == 0 && IsBuilt(module);
        }

        public bool CanReload(Module module)
        {
            return !IsEditorAssembly(module.Name) && (!module.HaveDevModule || !IsEditorAssembly(module.TypeO.DevModuleName));
        }

        public void LoadAssembly(Project project, Module module)
        {
            if (!LoadAssemblyFiles(module)) return;

            InitializeAssembly(project, module);
        }

        public void UnloadAssembly(Module module)
        {
            if (module.Assembly == null && module.DevAssembly == null) return;

            UninitializeTypeD(module);
            Unregister(module.Assembly);
            Unregister(module.DevAssembly);
            module.Assembly = null;
            module.DevAssembly = null;
            module.ModuleTypeInfo = null;
            module.TypeDModuleInitializer = null;
            module.Initialized = false;
        }

        public void UnloadAssemblies(Project project)
        {
            // Modules deinitialize in the reverse of the order they initialized in
            var order = InitializationOrder(project);
            for (var i = order.Count - 1; i >= 0; i--)
            {
                UnloadAssembly(order[i]);
            }
        }

        public void ReloadAssemblies(Project project, IEnumerable<Module> modules, Action<Module, int, int> progress = null)
        {
            // A reloaded module invalidates every module initialized after it, so the earliest requested
            // module and everything loaded after it in the initialization order reload together
            var order = InitializationOrder(project);
            var requested = modules.Where(m => order.Contains(m) && CanReload(m)).ToHashSet();
            if (requested.Count == 0) return;

            var reloading = order.Skip(order.FindIndex(requested.Contains))
                .Where(m => requested.Contains(m) || (m.Assembly != null && CanReload(m))).ToList();
            for (var i = reloading.Count - 1; i >= 0; i--)
            {
                UnloadAssembly(reloading[i]);
            }

            // Every assembly is loaded before any is initialized, since initializing resolves the modules it depends on
            var loaded = reloading.Where(LoadAssemblyFiles).ToList();
            for (var i = 0; i < loaded.Count; i++)
            {
                progress?.Invoke(loaded[i], i, loaded.Count);
                InitializeAssembly(project, loaded[i]);
            }
        }

        // The order the modules initialize in: every module after the modules it depends on, and modules
        // without dependencies between them in project order; deinitialization runs in the reverse order
        private static List<Module> InitializationOrder(Project project)
        {
            var byName = new Dictionary<string, Module>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in project.Modules) byName.TryAdd(module.Name, module);

            var order = new List<Module>();
            var visited = new HashSet<Module>();
            void Visit(Module module)
            {
                // An already visited module is initialized by an earlier dependent; a dependency cycle keeps the project order
                if (!visited.Add(module)) return;
                foreach (var dependency in ModuleDependencies(module, byName)) Visit(dependency);
                order.Add(module);
            }
            foreach (var module in project.Modules) Visit(module);
            return order;
        }

        // The project modules a module depends on, from the dependencies in its product description
        private static IEnumerable<Module> ModuleDependencies(Module module, Dictionary<string, Module> byName)
        {
            foreach (var dependency in module.TypeO?.Dependencies ?? Enumerable.Empty<string>())
            {
                // A dependency is written 'Author@Name-Version[;tag...]', where the first '-' separates name and version
                var name = dependency.Split(';')[0];
                var version = name.IndexOf('-');
                if (version >= 0) name = name[..version];
                var author = name.IndexOf('@');
                if (author >= 0) name = name[(author + 1)..];
                if (byName.TryGetValue(name, out var found) && found != module) yield return found;
            }
        }

        // Assembly loading
        private static readonly ConcurrentDictionary<string, Assembly> ModuleAssemblies = new(StringComparer.OrdinalIgnoreCase);
        // The modules of the open project by assembly name, so their assemblies can be loaded on demand
        private static readonly ConcurrentDictionary<string, Module> KnownModules = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object OnDemandLoadLock = new();
        private static readonly Lazy<HashSet<string>> EditorAssemblies = new(() =>
        {
            var appdata = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
            return new HashSet<string>((appdata)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase);
        });

        static ModuleModel()
        {
            // Lets the game, which is loaded in its own context, and the editor find the latest version of a module
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => FindModuleAssembly(new AssemblyName(e.Name));
        }

        // The editor is built with these assemblies, so they are shared with it instead of loaded again
        internal static bool IsEditorAssembly(string name)
        {
            return name != null && EditorAssemblies.Value.Contains(name);
        }

        private static Assembly FindModuleAssembly(AssemblyName name)
        {
            if (name.Name == null) return null;
            if (ModuleAssemblies.TryGetValue(name.Name, out var assembly)) return assembly;
            return LoadKnownModuleAssembly(name.Name);
        }

        internal static bool IsKnownModuleAssembly(string name)
        {
            return name != null && KnownModules.ContainsKey(name);
        }

        // A module assembly requested before its module has loaded (while an add or reload is in progress) is
        // loaded on demand, so the requester is never bound to a copy from another module's bin folder
        private static Assembly LoadKnownModuleAssembly(string name)
        {
            if (!KnownModules.TryGetValue(name, out var module)) return null;
            lock (OnDemandLoadLock)
            {
                if (ModuleAssemblies.TryGetValue(name, out var loaded)) return loaded;

                var isDev = !name.Equals(module.Name, StringComparison.OrdinalIgnoreCase);
                var path = isDev ? module.ModuleDevDLLPath : module.ModuleDLLPath;
                if (!File.Exists(path)) return null;

                var assembly = LoadModuleAssembly(module, path);
                if (isDev) module.DevAssembly = assembly;
                else module.Assembly = assembly;
                return assembly;
            }
        }

        private bool LoadAssemblyFiles(Module module)
        {
            if (!File.Exists(module.ModuleDLLPath)) return false;

            // An assembly already loaded on demand is kept, so every consumer binds the same instance
            module.Assembly ??= LoadModuleAssembly(module, module.ModuleDLLPath);
            if (module.HaveDevModule && module.DevAssembly == null && File.Exists(module.ModuleDevDLLPath))
            {
                module.DevAssembly = LoadModuleAssembly(module, module.ModuleDevDLLPath);
            }
            return true;
        }

        private void InitializeAssembly(Project project, Module module)
        {
            module.ModuleTypeInfo = GetModuleType(module);
            InitializeTypeD(project, module);
            module.Initialized = true;
        }

        private static Assembly LoadModuleAssembly(Module module, string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var assembly = IsEditorAssembly(name)
                ? Assembly.LoadFrom(path)
                : new ModuleLoadContext($"{module.Name}: {name}", Path.GetDirectoryName(path), FindModuleAssembly).LoadFile(path);
            ModuleAssemblies[name] = assembly;
            return assembly;
        }

        private static void Unregister(Assembly assembly)
        {
            if (assembly == null) return;

            var name = assembly.GetName().Name;
            if (name != null && ModuleAssemblies.TryGetValue(name, out var registered) && registered == assembly)
            {
                ModuleAssemblies.TryRemove(name, out _);
            }
        }

        private static string BuildTarget(Module module)
        {
            var solutionSln = Path.Combine(module.ModulePath, $"{module.Name}.sln");
            var solutionSlnx = Path.Combine(module.ModulePath, $"{module.Name}.slnx");
            var solution = File.Exists(solutionSln) ? solutionSln : (File.Exists(solutionSlnx) ? solutionSlnx : null);
            var target = solution ?? Path.Combine(module.ModulePath, module.Name, $"{module.Name}.csproj");
            return File.Exists(target) ? target : null;
        }

        private static async Task<int> RunBuild(Module module, string target, string verbosity, Action<string> output)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = module.ModulePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // NuGet assemblies are copied next to the module dll, since ModuleLoadContext resolves from that
            // folder and the editor does not ship the packages the modules use
            foreach (var arg in new[] { "build", target, "-c", "Debug", "--verbosity", verbosity, "-p:CopyLocalLockFileAssemblies=true" }) startInfo.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) output?.Invoke(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) output?.Invoke(e.Data); };
            if (!process.Start())
                throw new InvalidOperationException("The dotnet build process could not be started.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            process.WaitForExit();
            return process.ExitCode;
        }

        // Internal functions
        private async Task<bool> Apply(Project project, bool changed)
        {
            if (!changed) return false;

            var resolved = await Resolve(project, true);
            SaveModel.AddSave<ProjectSaveContext>(project);
            var projectModel = ResourceModel.Get<IProjectModel>();
            projectModel.RefreshWatching(project);
            projectModel.InitAndSaveCode(project, new ProgramCode());
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
                    // Dev-tagged module dependencies are included, since the dev assemblies run inside the
                    // editor and their dependencies must resolve to loaded modules rather than bin folder copies
                    if (dependency.Nuget || dependency.Tags.Any(tag => tag != "dev") || dependency.Name == "TypeD") continue;
                    if (!visited.Add(dependency.Name)) continue;

                    // Overrides declared by a module are relative to that module, so make the path absolute
                    var version = dependency.IsLocal ? $"{Path.GetFullPath(dependency.LocalPath, path)};local" : dependency.Version;
                    var module = project.Modules.Find(m => m.Name == dependency.Name && m.Version == version)
                        ?? new Module() { Name = dependency.Name, Author = dependency.Author, Version = version };
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

            // A removed module invalidates every module initialized after it, so everything from the first removed
            // module onward unloads in reverse order; the kept ones are loaded and initialized again by the resolve
            var order = InitializationOrder(project);
            var firstRemoved = order.FindIndex(m => !modules.Contains(m));
            if (firstRemoved >= 0)
            {
                for (var i = order.Count - 1; i >= firstRemoved; i--)
                {
                    UnloadAssembly(order[i]);
                }
            }
            project.Modules.Clear();
            project.Modules.AddRange(modules);

            // Known modules are updated without a clear, so a concurrent resolve never misses an entry
            foreach (var module in modules)
            {
                KnownModules[module.Name] = module;
                if (module.HaveDevModule) KnownModules[module.TypeO.DevModuleName] = module;
            }
            foreach (var stale in KnownModules.Where(known => !modules.Contains(known.Value)).Select(known => known.Key).ToList())
            {
                KnownModules.TryRemove(stale, out _);
            }
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
            public string Author { get; set; }
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
                    Modules = project.Modules.Select(m => new ModuleCacheEntry() { Name = m.Name, Author = m.Author, Version = m.Version }).ToList()
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
            
            // Check for both .sln and .slnx files
            var basePath = Path.Combine(project.Location, Path.GetFileNameWithoutExtension(project.CSSolutionPath));
            var slnPath = basePath + ".sln";
            var slnxPath = basePath + ".slnx";
            if (File.Exists(slnPath))
                AddStamp(slnPath);
            else if (File.Exists(slnxPath))
                AddStamp(slnxPath);
            else
                AddStamp(slnPath); // Default to .sln path even if it doesn't exist (for hashing consistency)
            
            return Hash(hash.ToString());
        }

        // The source files of every local module, since those are the ones that change between loads; released modules are immutable
        private static string BuildStamp(Project project)
        {
            var hash = new StringBuilder();
            var skipped = new[] { "bin", "obj", ".git", ".vs", "typer", "node_modules" };
            var extensions = new[] { ".cs", ".csproj", ".axaml", ".xaml", ".resx", ".json", ".props", ".targets", ".sln", ".slnx" };
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
            return IsBuiltWithDependencies(module.ModuleDLLPath) && (!module.HaveDevModule || IsBuiltWithDependencies(module.ModuleDevDLLPath));
        }

        // A module built outside the editor (IDE, dotnet build, CI) does not copy its NuGet assemblies next to the
        // module assembly and the incremental clean even removes earlier copies, so such a module counts as not
        // built and is built again by the editor, which restores the NuGet assemblies the editor must load
        private static bool IsBuiltWithDependencies(string dllPath)
        {
            if (!File.Exists(dllPath)) return false;

            var folder = Path.GetDirectoryName(dllPath);
            return PackageRuntimeAssemblies(dllPath).All(assembly => File.Exists(Path.Combine(folder, assembly)));
        }

        // The NuGet assemblies the module needs at runtime, from the deps file its build writes
        private static IEnumerable<string> PackageRuntimeAssemblies(string dllPath)
        {
            var depsPath = Path.ChangeExtension(dllPath, ".deps.json");
            if (!File.Exists(depsPath)) return Enumerable.Empty<string>();

            try
            {
                using var deps = JsonDocument.Parse(File.ReadAllText(depsPath));
                var assemblies = new List<string>();
                if (!deps.RootElement.TryGetProperty("targets", out var targets) ||
                    !deps.RootElement.TryGetProperty("libraries", out var libraries)) return assemblies;

                foreach (var target in targets.EnumerateObject())
                {
                    foreach (var library in target.Value.EnumerateObject())
                    {
                        // Only package assemblies are copied next to the module assembly; projects load as modules or editor assemblies
                        if (!libraries.TryGetProperty(library.Name, out var info) ||
                            !info.TryGetProperty("type", out var type) || type.GetString() != "package") continue;
                        if (!library.Value.TryGetProperty("runtime", out var runtime)) continue;

                        foreach (var assembly in runtime.EnumerateObject())
                        {
                            var file = Path.GetFileName(assembly.Name);
                            if (file != "_._") assemblies.Add(file);
                        }
                    }
                }
                return assemblies;
            }
            catch (Exception)
            {
                // An unreadable deps file does not fail the module, the build check then only requires the module assembly
                return Enumerable.Empty<string>();
            }
        }

        private void InitializeTypeD(Project project, Module module)
        {
            if (!module.HaveDevModule || module.DevAssembly == null) return;

            var typeDInitType = GetLoadableTypes(module.DevAssembly).FirstOrDefault(t => { return t.IsSubclassOf(typeof(TypeDModuleInitializer)); });
            if (typeDInitType == null) return;

            module.TypeDModuleInitializer = Activator.CreateInstance(typeDInitType) as TypeDModuleInitializer;
            module.TypeDModuleInitializer.Hooks = HookModel;
            module.TypeDModuleInitializer.Resources = ResourceModel;
            try
            {
                var versions = ResourceModel.Get<IFormatVersionModel>();
                foreach (var definition in module.TypeDModuleInitializer.FormatVersions)
                    module.FormatVersionRegistrations.Add(versions.Register(module.Name, definition));
                module.TypeDModuleInitializer.Initializer(project);
            }
            catch
            {
                foreach (var registration in module.FormatVersionRegistrations)
                    registration.Dispose();
                module.FormatVersionRegistrations.Clear();
                throw;
            }
        }

        private void UninitializeTypeD(Module module)
        {
            // The stored initializer is used instead of reflecting over the assembly, since a module whose
            // references can no longer be resolved must still be possible to unload
            try
            {
                module.TypeDModuleInitializer?.Uninitializer();
            }
            finally
            {
                foreach (var registration in module.FormatVersionRegistrations)
                    registration.Dispose();
                module.FormatVersionRegistrations.Clear();
            }
        }

        // Skips the types a module cannot load, so one built against assemblies the editor cannot resolve does not crash loading
        private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                LogModel.Log($"Some types in {assembly.GetName().Name} could not be loaded: " +
                    string.Join(" ", e.LoaderExceptions.Where(x => x != null).Select(x => x.Message).Distinct()));
                return e.Types.Where(t => t != null);
            }
        }

        private System.Reflection.TypeInfo GetModuleType(Module module)
        {
            if (module.Name == "TypeOCore") return null;
            System.Reflection.TypeInfo moduleType = null;
            foreach (var type in GetLoadableTypes(module.Assembly))
            {
                if (type?.BaseType?.FullName?.StartsWith("TypeOEngine.Typedeaf.Core.Engine.Module") == true)
                {
                    moduleType = type.GetTypeInfo();
                    break;
                }
            }

            return moduleType;
        }
    }
}
