using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using TypeD.Models.Data;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class TyperModel : ITyperModel
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Models
        ILogModel LogModel { get; set; }

        // Properties
        public bool IsAvailable { get { return TyperScriptPath != null; } }
        private string TyperScriptPath { get; set; }

        // Constructors
        public TyperModel() { }

        public void Init(IResourceModel resourceModel)
        {
            LogModel = resourceModel.Get<ILogModel>();
            TyperScriptPath = FindTyper(AppContext.BaseDirectory, Environment.GetEnvironmentVariable("TYPER_PATH"));
        }

        // Functions
        public async Task<IEnumerable<ModuleList>> ListModules()
        {
            var result = await Run("modules");
            var modules = Parse<List<TyperModuleDTO>>(result);
            return (modules ?? new List<TyperModuleDTO>()).Select(m => new ModuleList()
            {
                Name = m.Name,
                Author = m.Author,
                Versions = m.Versions.Select(v => new ModuleTypeO() { Version = v.Version }).ToList()
            }).ToList();
        }

        public async Task<IEnumerable<ModuleDependency>> ListDependencies(string projectPath)
        {
            var result = await Run("dependencies", "-p", projectPath);
            var dependencies = Parse<List<ModuleDependency>>(result);
            return dependencies ?? new List<ModuleDependency>();
        }

        // Module versions are immutable, so a fetched product description is kept for the lifetime of the editor
        private readonly ConcurrentDictionary<string, ModuleTypeO> moduleTypeOCache = new();

        public async Task<ModuleTypeO> GetModuleTypeO(string author, string name, string version)
        {
            if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version)) return null;

            var key = $"{author}@{name}-{version}";
            if (moduleTypeOCache.TryGetValue(key, out var cached)) return cached;

            var dto = Parse<ModuleListModuleDTO>(await Run("moduleinfo", "-a", author, "-n", name, "-v", version));
            if (dto == null) return null;

            var typeO = new ModuleTypeO(dto);
            moduleTypeOCache[key] = typeO;
            return typeO;
        }

        public async Task<bool> AddDependency(string projectPath, string dependency)
        {
            return (await Run("adddependency", "-p", projectPath, "-d", dependency)).Success;
        }

        public async Task<bool> RemoveDependency(string projectPath, string name)
        {
            return (await Run("removedependency", "-p", projectPath, "-n", name)).Success;
        }

        public async Task<bool> UpdateDependency(string projectPath, string name = null, string version = null)
        {
            var args = new List<string> { "update", "-p", projectPath };
            if (!string.IsNullOrEmpty(name)) args.AddRange(new[] { "-n", name });
            if (!string.IsNullOrEmpty(version)) args.AddRange(new[] { "-v", version });
            return (await Run(args.ToArray())).Success;
        }

        public async Task<bool> AddOverride(string projectPath, string name, string localPath)
        {
            return (await Run("addoverride", "-p", projectPath, "-n", name, "-l", localPath)).Success;
        }

        public async Task<bool> RemoveOverride(string projectPath, string name)
        {
            return (await Run("removeoverride", "-p", projectPath, "-n", name)).Success;
        }

        public Task<bool> Resolve(string projectPath, Action<string> onOutput = null)
        {
            return Resolve(projectPath, null, onOutput);
        }

        public async Task<bool> Resolve(string projectPath, string resolvedModulesPath, Action<string> onOutput)
        {
            var args = new List<string> { "dependency", "-p", projectPath };
            if (resolvedModulesPath != null)
                args.AddRange(new[] { "--resolvedModules", resolvedModulesPath });
            return (await Run(onOutput, args.ToArray())).Success;
        }

        public async Task<bool> CreateModule(string name, string outputPath, IEnumerable<string> dependencies = null)
        {
            var args = new List<string> { "new", "-n", name, "-o", outputPath, "-t", "module", "--noCommit" };
            if (dependencies != null && dependencies.Any())
            {
                args.AddRange(new[] { "-d", string.Join(" ", dependencies) });
            }
            return (await Run(args.ToArray())).Success;
        }

        // Internal functions
        private record TyperResult(bool Success, string Output);

        private T Parse<T>(TyperResult result) where T : class
        {
            if (!result.Success) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(result.Output, JsonOptions);
            }
            catch (JsonException)
            {
                LogModel?.Log($"typer returned unexpected output: {result.Output}");
                return null;
            }
        }

        private Task<TyperResult> Run(params string[] args)
        {
            return Run(null, args);
        }

        private async Task<TyperResult> Run(Action<string> onOutput, params string[] args)
        {
            if (!IsAvailable)
            {
                LogModel?.Log("typer could not be found, set TYPER_PATH to the typer folder");
                return new TyperResult(false, "");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "py" : "python3",
                WorkingDirectory = Path.GetDirectoryName(TyperScriptPath),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(TyperScriptPath);
            foreach (var arg in args) startInfo.ArgumentList.Add(arg);

            try
            {
                using var process = Process.Start(startInfo);
                process.StandardInput.Close();
                var output = process.StandardOutput.ReadToEndAsync();
                var errorLines = new List<string>();
                var error = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0) continue;
                        errorLines.Add(line);
                        try { onOutput?.Invoke(line); } catch { }
                    }
                });
                await process.WaitForExitAsync();
                await error;

                LogModel?.Log($"typer {string.Join(" ", args)}");
                foreach (var line in errorLines)
                {
                    LogModel?.Log(line);
                }
                return new TyperResult(process.ExitCode == 0, await output);
            }
            catch (Exception e)
            {
                LogModel?.Log($"Failed to run typer: {e.Message}");
                return new TyperResult(false, "");
            }
        }

        private static string FindTyper(string applicationPath, string configuredPath)
        {
            if (!string.IsNullOrEmpty(configuredPath) && File.Exists(Path.Combine(configuredPath, "typer.py")))
            {
                return Path.Combine(configuredPath, "typer.py");
            }

            // Prefer the top-level checkout over TypeR copies nested inside editor or module repositories.
            string scriptPath = null;
            for (var dir = new DirectoryInfo(applicationPath); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "typer", "typer.py");
                if (File.Exists(candidate)) scriptPath = candidate;
            }
            return scriptPath;
        }
    }
}
