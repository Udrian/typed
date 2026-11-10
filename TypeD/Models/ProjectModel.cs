using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using TypeD.View.TreeNodes;

namespace TypeD.Models
{
    internal class ProjectModel : IProjectModel
    {
        // Models
        IModuleModel ModuleModel { get; set; }
        IHookModel HookModel { get; set; }
        ISaveModel SaveModel { get; set; }
        IResourceModel ResourceModel { get; set; }
        ILogModel LogModel { get; set; }
        private readonly ConcurrentDictionary<Project, ProjectWatch> ProjectWatchers = new();
        private readonly ConditionalWeakTable<Project, SemaphoreSlim> BuildLocks = new();
        private static readonly Regex BuildDiagnosticRegex = new(
            @"^(?:(?<file>.+?)(?:\((?<line>\d+)(?:,(?<column>\d+))?\))?:\s*)?(?<severity>warning|error)(?:\s+(?<code>[^:]+))?:\s*(?<message>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BuildProjectSuffixRegex = new(
            @"\s*\[(?<project>[^\[\]]+?\.\w*proj)(?:::[^\]]*)?\]$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // MSBuild reports every project it finished building as "  Project -> path\Project.dll"
        private static readonly Regex BuildProjectOutputRegex = new(
            @"^\s*(?<project>[\w.\-]+) -> \S.*$",
            RegexOptions.Compiled);
        private static readonly string[] IgnoredProjectFolders = { "bin", "obj", ".git", ".vs", "typer", "node_modules" };

        // Providers
        IComponentProvider ComponentProvider { get; set; }

        // Constructors
        public ProjectModel() { }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            ModuleModel = ResourceModel.Get<IModuleModel>();
            HookModel = ResourceModel.Get<IHookModel>();
            SaveModel = ResourceModel.Get<ISaveModel>();
            ComponentProvider = ResourceModel.Get<IComponentProvider>();
            LogModel = ResourceModel.Get<ILogModel>();
        }

        // Functions
        public Task<bool> Build(Project project)
        {
            return Build(project, null);
        }

        public async Task<bool> Build(Project project, IEnumerable<Module> changedModules)
        {
            var buildLock = BuildLocks.GetValue(project, _ => new SemaphoreSlim(1, 1));
            await buildLock.WaitAsync();

            var modules = (changedModules ?? Enumerable.Empty<Module>()).Where(project.Modules.Contains).Distinct().ToList();
            var reloadModules = modules.Any(ModuleModel.CanReload);
            var run = new ProjectBuildRun(HookModel, project, modules.Count + 1 + (reloadModules ? 1 : 0));
            try
            {
                if (!File.Exists(project.ProjectCSProjPath))
                {
                    run.AddIssue(GameIssue(project, "error", $"Project file not found: '{project.ProjectCSProjPath}'."));
                    run.Complete(false, "Build failed");
                    return false;
                }

                for (var i = 0; i < modules.Count; i++)
                {
                    var module = modules[i];
                    var count = modules.Count > 1 ? $" ({i + 1}/{modules.Count})" : string.Empty;
                    run.BeginStep($"Building module {module.Name}{count}", $"Compiling {module.Name}", CountProjects(module.ModulePath));
                    if (!ModuleModel.CanReload(module))
                    {
                        run.AddIssue(ModuleIssue(module, "warning",
                            $"{module.Name} is part of the editor, so the editor keeps using the version it was built with. " +
                            "Rebuild and restart the editor to use these changes inside the editor."));
                    }

                    if (!await ModuleModel.Rebuild(module, line => HandleBuildOutput(project, line, run, module)))
                    {
                        if (run.ErrorCount == 0)
                            run.AddIssue(ModuleIssue(module, "error", $"Module {module.Name} failed to build."));
                        run.Complete(false, $"Build failed in module {module.Name}");
                        return false;
                    }
                }

                run.BeginStep($"Building {project.ProjectName}", $"Compiling {project.ProjectName}", CountGameProjects(project));
                var exitCode = await RunGameBuild(project, run);
                if (exitCode != 0)
                {
                    if (run.ErrorCount == 0)
                        run.AddIssue(GameIssue(project, "error", $"Build failed with exit code {exitCode}."));
                    run.Complete(false, "Build failed");
                    return false;
                }

                bool succeeded;
                if (reloadModules)
                {
                    run.BeginStep("Reloading modules", "Stopping the game");
                    succeeded = await RunOnUIThread(() => ReloadGameAndModules(project, modules, run));
                }
                else
                {
                    succeeded = LoadAssembly(project);
                }

                if (!succeeded && run.ErrorCount == 0)
                    run.AddIssue(GameIssue(project, "error", "Build completed, but the project assembly could not be loaded."));

                run.Complete(succeeded, succeeded ? (reloadModules ? "Build succeeded, modules reloaded" : "Build succeeded") : "Build failed");
                return succeeded;
            }
            catch (Exception exception)
            {
                LogModel.Log($"Project build failed: {exception}");
                run.AddIssue(GameIssue(project, "error", exception.Message));
                run.Complete(false, "Build failed");
                return false;
            }
            finally
            {
                buildLock.Release();
            }
        }

        public void StartWatching(Project project)
        {
            if (ProjectWatchers.TryGetValue(project, out var existing))
            {
                existing.Refresh();
                return;
            }

            var watcher = new ProjectWatch(project, RequestWatchedBuild, LogModel);
            if (!ProjectWatchers.TryAdd(project, watcher))
                watcher.Dispose();
        }

        public void RefreshWatching(Project project)
        {
            if (ProjectWatchers.TryGetValue(project, out var watcher))
                watcher.Refresh();
        }

        public void StopWatching(Project project)
        {
            if (ProjectWatchers.TryRemove(project, out var watcher))
                watcher.Dispose();
        }

        private void RequestWatchedBuild(Project project, IReadOnlyList<Module> changedModules)
        {
            if (project.IsClosing)
                return;

            _ = Build(project, changedModules);
        }

        private async Task<int> RunGameBuild(Project project, ProjectBuildRun run)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = project.Location,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("build");
            startInfo.ArgumentList.Add(project.ProjectCSProjPath);
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(project.ProjectBuildOutputPath);
            startInfo.ArgumentList.Add("--verbosity");
            startInfo.ArgumentList.Add("minimal");

            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) => HandleBuildOutput(project, e.Data, run, null);
            process.ErrorDataReceived += (_, e) => HandleBuildOutput(project, e.Data, run, null);

            if (!process.Start())
            {
                throw new InvalidOperationException("The dotnet build process could not be started.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            process.WaitForExit();
            return process.ExitCode;
        }

        // Stops the game, loads the rebuilt modules and the game again, then starts the game with them
        private bool ReloadGameAndModules(Project project, List<Module> changedModules, ProjectBuildRun run)
        {
            HookModel.Shoot(new GameStopHook { Project = project });

            // The module model also reloads every loaded module initialized after the changed ones,
            // so modules that use a changed module are bound to its new version as well
            try
            {
                ModuleModel.ReloadAssemblies(project, changedModules, (module, index, count) =>
                {
                    LogModel.Log($"Reloading module {module.Name}");
                    run.UpdateSubTask($"Loading module {module.Name} ({index + 1}/{count})", 100 * index / Math.Max(count + 1, 1));
                });
            }
            catch (Exception exception)
            {
                LogModel.Log($"Reloading modules failed: {exception}");
                run.AddIssue(GameIssue(project, "error", $"Reloading the modules failed: {exception.Message}"));
                return false;
            }

            run.UpdateSubTask($"Loading {project.ProjectName}", 95);
            if (!LoadAssembly(project))
                return false;

            run.UpdateSubTask("Starting the game", 100);
            HookModel.Shoot(new GameStartHook { Project = project });
            return true;
        }

        // Module initializers and the viewers create UI, so the reload runs on the UI thread when the editor is running
        private static async Task<bool> RunOnUIThread(Func<bool> action)
        {
            if (Avalonia.Application.Current == null || Dispatcher.UIThread.CheckAccess())
                return action();

            return await Dispatcher.UIThread.InvokeAsync(action);
        }

        private void HandleBuildOutput(Project project, string output, ProjectBuildRun run, Module buildingModule)
        {
            if (output == null)
                return;

            CMD.ReportOutput(output);

            var built = BuildProjectOutputRegex.Match(output);
            if (built.Success)
            {
                run.ProjectBuilt(built.Groups["project"].Value);
                return;
            }

            var issue = ParseBuildIssue(output);
            if (issue == null)
                return;

            var workingDirectory = buildingModule?.ModulePath ?? project.Location;
            var module = FindIssueModule(project, issue, workingDirectory) ?? buildingModule;
            issue.Source = module?.Name ?? project.ProjectName;
            issue.IsModuleIssue = module != null;
            run.AddIssue(issue);
        }

        internal static BuildIssue ParseBuildIssue(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return null;

            var match = BuildDiagnosticRegex.Match(output.Trim());
            if (!match.Success)
                return null;

            // MSBuild ends a diagnostic with the project that reported it, e.g. " [D:\Game\Game.csproj::TargetFramework=net10.0]"
            var message = match.Groups["message"].Value.Trim();
            var projectFile = BuildProjectSuffixRegex.Match(message);
            if (projectFile.Success)
                message = message.Substring(0, projectFile.Index).TrimEnd();

            return new BuildIssue
            {
                Severity = match.Groups["severity"].Value.ToLowerInvariant(),
                Code = match.Groups["code"].Value.Trim(),
                Message = message,
                File = match.Groups["file"].Value.Trim(),
                Line = int.TryParse(match.Groups["line"].Value, out var line) ? line : null,
                Column = int.TryParse(match.Groups["column"].Value, out var column) ? column : null,
                ProjectFile = projectFile.Success ? projectFile.Groups["project"].Value : null
            };
        }

        // Finds the module the reporting project or file is part of, the game project's own files are checked first
        internal static Module FindIssueModule(Project project, BuildIssue issue, string workingDirectory)
        {
            foreach (var path in new[] { issue.ProjectFile, issue.File })
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(path, workingDirectory);
                }
                catch (Exception)
                {
                    continue;
                }

                if (IsWithin(fullPath, Path.GetDirectoryName(project.ProjectCSProjPath)))
                    return null;

                var module = project.Modules.ToList()
                    .Where(m => !string.IsNullOrEmpty(m.Name) && !string.IsNullOrEmpty(m.Version) && IsWithin(fullPath, m.ModulePath))
                    .OrderByDescending(m => m.ModulePath.Length)
                    .FirstOrDefault();
                if (module != null)
                    return module;
            }

            return null;
        }

        private static bool IsWithin(string path, string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return false;

            folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
            return path.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static BuildIssue GameIssue(Project project, string severity, string message)
        {
            return new BuildIssue { Severity = severity, Message = message, Source = project.ProjectName, ProjectFile = project.ProjectCSProjPath };
        }

        private static BuildIssue ModuleIssue(Module module, string severity, string message)
        {
            return new BuildIssue { Severity = severity, Message = message, Source = module.Name, IsModuleIssue = true };
        }

        // The number of C# projects a module build is expected to compile, used to show the progress of the build
        private static int? CountProjects(string folder)
        {
            if (!Directory.Exists(folder))
                return null;

            var count = 0;
            var pending = new Stack<string>();
            pending.Push(folder);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                count += Directory.EnumerateFiles(current, "*.csproj").Count();
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    if (!IgnoredProjectFolders.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase))
                        pending.Push(directory);
                }
            }
            return count > 0 ? count : null;
        }

        private static int? CountGameProjects(Project project)
        {
            try
            {
                return 1 + Regex.Matches(File.ReadAllText(project.ProjectCSProjPath), "<ProjectReference\\b").Count;
            }
            catch (IOException)
            {
                return null;
            }
        }

        public void Run(Project project)
        {
            var fileToRun = Path.Combine(project.ProjectBuildOutputPath, $"{project.CSProjName}.exe");
            if(File.Exists(fileToRun))
            {
                // The game finds its external files (e.g. shaders) relative to the project folder
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileToRun,
                    WorkingDirectory = project.Location
                });
            }
            else
            {
                LogModel.Log($"File '{fileToRun}' does not exists, try building the project.");
            }
        }

        public void InitCode(Project project, Codalyzer code)
        {
            code.Project = project;
            code.Resources = ResourceModel;
            code.Init();
            if (!code.Initialized) throw new Exception($"Codalyzer '{code.GetType().FullName}' not initialized");
        }

        public void SaveCode(Codalyzer code)
        {
           var codeSaveContext = SaveModel.GetSaveContext<CodeSaveContext>();
            if (codeSaveContext.Codes.Exists(c => c.FilePath() == code.FilePath()))
                codeSaveContext.Codes.RemoveAll(c => c.FilePath() == code.FilePath());
            codeSaveContext.Codes.Add(code);
            SaveModel.AddSave<CodeSaveContext>();
        }

        public void InitAndSaveCode(Project project, Codalyzer code)
        {
            InitCode(project, code);

            SaveCode(code);
        }

        public void BuildComponentTree(Project project)
        {
            project.ComponentTree.Clear();

            var components = ComponentProvider.ListAll(project);
            foreach (var component in components)
            {
                AddComponentToTree(project, component);
            }

            HookModel.Shoot(new ComponentTreeBuiltHook(project.ComponentTree));
        }

        public bool LoadAssembly(Project project)
        {
            var path = Path.Combine(project.ProjectBuildOutputPath, $"{project.CSProjName}.dll");
            if (!File.Exists(path)) return false;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                fs.ReadExactly(bytes);

                project.Assembly = System.Reflection.Assembly.Load(bytes);
            }

            return true;
        }

        public string TransformNamespaceString(Project project, string @namespace)
        {
            return (@namespace.StartsWith(project.ProjectName) ? @namespace : $"{project.ProjectName}.{@namespace}").Replace("\\", ".").Replace("/", ".");
        }

        // Internal
        private void AddComponentToTree(Project project, Component component)
        {
            var namespaces = (component.Namespace.StartsWith(project.ProjectName) ? component.Namespace.Remove(0, project.ProjectName.Length) : component.Namespace).Split('.').ToList();
            if (namespaces.Count > 0)
                namespaces.RemoveAt(0);

            TreeNode treeNode = project.ComponentTree;
            foreach (var ns in namespaces)
            {
                if (string.IsNullOrEmpty(ns)) continue;
                var nskey = string.Join(".", namespaces.Take(namespaces.FindIndex(n => n == ns) + 1));
                if (!treeNode.Contains(nskey))
                {
                    treeNode.AddNode(ns, nskey, "namespace");
                }
                treeNode = treeNode.Get(nskey);
            }

            if(ComponentProvider.Exists(project, component))
            {
                treeNode.AddNode(component.ClassName, component.FullName, component);
            }
            else
            {
                treeNode.AddNode($"*{component.ClassName}", component.FullName, component);
            }
        }
    }
}
