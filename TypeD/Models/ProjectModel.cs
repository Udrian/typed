using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ConcurrentDictionary<Project, ProjectFileWatcher> ProjectWatchers = new();
        private readonly ConditionalWeakTable<Project, SemaphoreSlim> BuildLocks = new();
        private static readonly Regex BuildDiagnosticRegex = new(
            @"^(?:(?<file>.+?)(?:\((?<line>\d+)(?:,(?<column>\d+))?\))?:\s*)?(?<severity>warning|error)(?:\s+(?<code>[^:]+))?:\s*(?<message>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        public void AddModule(Project project, Module module)
        {
            if (!File.Exists(project.ProjectCSProjPath)) return;
            if (project.Modules.Exists(m => m.Name == module.Name && m.Version == module.Version)) return;

            var projectCSProjSaveContext = SaveModel.GetSaveContext<ProjectCSProjSaveContext>(project.ProjectCSProjPath);
            if (project.Modules.Exists(m => m.Name == module.Name))
            {
                var removedModule = project.Modules.Find(m => m.Name == module.Name);
                project.Modules.Remove(removedModule);
                ModuleModel.RemoveFromProjectXML(removedModule, projectCSProjSaveContext.CSProj);
                ModuleModel.UnloadAssembly(removedModule);
            }

            project.Modules.Add(module);
            SaveModel.AddSave<ProjectSaveContext>(project);
            ModuleModel.AddToProjectXML(module, projectCSProjSaveContext.CSProj);
            SaveModel.AddSave<ProjectCSProjSaveContext>();

            InitAndSaveCode(project, new ProgramCode());
        }

        public void RemoveModule(Project project, string moduleName)
        {
            if (!File.Exists(project.ProjectCSProjPath)) return;
            var module = project.Modules.Find(m => m.Name == moduleName);
            project.Modules.Remove(module);

            SaveModel.AddSave<ProjectSaveContext>(project);

            var projectCSProjSaveContext = SaveModel.GetSaveContext<ProjectCSProjSaveContext>(project.ProjectCSProjPath);
            ModuleModel.RemoveFromProjectXML(module, projectCSProjSaveContext.CSProj);
            SaveModel.AddSave<ProjectCSProjSaveContext>();

            ModuleModel.UnloadAssembly(module);

            InitAndSaveCode(project, new ProgramCode());
        }

        public async Task<bool> Build(Project project)
        {
            var path = Path.Combine(project.Location, project.CSSolutionPath);
            var buildLock = BuildLocks.GetValue(project, _ => new SemaphoreSlim(1, 1));
            await buildLock.WaitAsync();
            var warnings = new List<BuildIssue>();
            var errors = new List<BuildIssue>();
            try
            {
                if (!File.Exists(path))
                {
                    errors.Add(new BuildIssue
                    {
                        Severity = "error",
                        Message = $"Solution file '{path}' does not exist."
                    });
                    PublishBuildStatus(project, false, true, false, "Build failed", warnings, errors);
                    return false;
                }

                PublishBuildStatus(project, true, false, false, "Building project...", warnings, errors);

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
                startInfo.ArgumentList.Add(path);
                startInfo.ArgumentList.Add("--output");
                startInfo.ArgumentList.Add(project.ProjectBuildOutputPath);
                startInfo.ArgumentList.Add("--verbosity");
                startInfo.ArgumentList.Add("minimal");

                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (_, e) => HandleBuildOutput(e.Data, warnings, errors);
                process.ErrorDataReceived += (_, e) => HandleBuildOutput(e.Data, warnings, errors);

                if (!process.Start())
                {
                    throw new InvalidOperationException("The dotnet build process could not be started.");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
                process.WaitForExit();

                var succeeded = process.ExitCode == 0 && LoadAssembly(project);
                if (process.ExitCode != 0 && errors.Count == 0)
                {
                    errors.Add(new BuildIssue
                    {
                        Severity = "error",
                        Message = $"Build failed with exit code {process.ExitCode}."
                    });
                }
                else if (!succeeded && errors.Count == 0)
                {
                    errors.Add(new BuildIssue
                    {
                        Severity = "error",
                        Message = "Build completed, but the project assembly could not be loaded."
                    });
                }

                PublishBuildStatus(
                    project,
                    false,
                    true,
                    succeeded,
                    succeeded ? "Build succeeded" : "Build failed",
                    warnings,
                    errors);
                return succeeded;
            }
            catch (Exception exception)
            {
                LogModel.Log($"Project build failed: {exception}");
                errors.Add(new BuildIssue { Severity = "error", Message = exception.Message });
                PublishBuildStatus(project, false, true, false, "Build failed", warnings, errors);
                return false;
            }
            finally
            {
                buildLock.Release();
            }
        }

        public void StartWatching(Project project)
        {
            if (ProjectWatchers.ContainsKey(project))
                return;

            var watcher = new ProjectFileWatcher(project, RequestWatchedBuild, LogModel);
            if (!ProjectWatchers.TryAdd(project, watcher))
                watcher.Dispose();
        }

        public void StopWatching(Project project)
        {
            if (ProjectWatchers.TryRemove(project, out var watcher))
                watcher.Dispose();
        }

        private void RequestWatchedBuild(Project project)
        {
            if (project.IsClosing)
                return;

            _ = Build(project);
        }

        private void PublishBuildStatus(
            Project project,
            bool isBuilding,
            bool isComplete,
            bool succeeded,
            string status,
            List<BuildIssue> warnings,
            List<BuildIssue> errors)
        {
            HookModel.Shoot(new BuildStatusChangedHook
            {
                Project = project,
                IsBuilding = isBuilding,
                IsComplete = isComplete,
                Succeeded = succeeded,
                Status = status,
                Warnings = new List<BuildIssue>(warnings),
                Errors = new List<BuildIssue>(errors)
            });
        }

        private static void AddBuildDiagnostic(string output, List<BuildIssue> warnings, List<BuildIssue> errors)
        {
            if (string.IsNullOrWhiteSpace(output))
                return;

            var match = BuildDiagnosticRegex.Match(output.Trim());
            if (!match.Success)
                return;

            var issue = new BuildIssue
            {
                Severity = match.Groups["severity"].Value.ToLowerInvariant(),
                Code = match.Groups["code"].Value.Trim(),
                Message = match.Groups["message"].Value.Trim(),
                File = match.Groups["file"].Value.Trim(),
                Line = int.TryParse(match.Groups["line"].Value, out var line) ? line : null,
                Column = int.TryParse(match.Groups["column"].Value, out var column) ? column : null
            };

            lock (warnings)
            {
                (issue.Severity == "warning" ? warnings : errors).Add(issue);
            }
        }

        private static void HandleBuildOutput(string output, List<BuildIssue> warnings, List<BuildIssue> errors)
        {
            if (output == null)
                return;

            CMD.ReportOutput(output);
            AddBuildDiagnostic(output, warnings, errors);
        }

        public void Run(Project project)
        {
            var fileToRun = Path.Combine(project.ProjectBuildOutputPath, $"{project.CSProjName}.exe");
            if(File.Exists(fileToRun))
            {
                Process.Start(fileToRun);
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
                fs.Read(bytes, 0, (int)fs.Length);

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
