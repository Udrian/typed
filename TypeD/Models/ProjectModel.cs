using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using TypeD.View.TreeNodes;
using TypeOEngine.Typedeaf.Core;

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
            if (!File.Exists(path)) return false;

            if (project.Assembly != null)
            {
                project.Assembly = null;
            }
            await CMD.Run($"dotnet build \"{path}\" --output \"{project.ProjectBuildOutputPath}\"");

            return LoadAssembly(project);
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

        public void SetStartScene(Project project, Component scene)
        {
            if (scene.TypeOBaseType != typeof(Scene)) return;
            project.StartScene = scene.FullName;

            SaveModel.AddSave<ProjectSaveContext>(project);

            var gameComponent = ComponentProvider.Load(project, $"{project.ProjectName}.{project.ProjectName}Game");

            SaveCode(gameComponent.Template.Code);
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
