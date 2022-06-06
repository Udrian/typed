using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ProjectProvider : IProjectProvider
    {
        // Models
        IResourceModel ResourceModel { get; set; }
        IProjectModel ProjectModel { get; set; }
        IModuleModel ModuleModel { get; set; }
        IHookModel HookModel { get; set; }
        ISaveModel SaveModel { get; set; }
        IRestoreModel RestoreModel { get; set; }

        // Providers
        IModuleProvider ModuleProvider { get; set; }

        // Constructors
        public ProjectProvider() { }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            ProjectModel = ResourceModel.Get<IProjectModel>();
            ModuleModel = ResourceModel.Get<IModuleModel>();
            HookModel = ResourceModel.Get<IHookModel>();
            SaveModel = ResourceModel.Get<ISaveModel>();
            RestoreModel = ResourceModel.Get<IRestoreModel>();

            ModuleProvider = ResourceModel.Get<IModuleProvider>();
        }

        // Functions
        public async Task<Project> Create(string projectName, string location, string csSolutionPath, string csProjName, Action<int> progress)
        {
            progress(0);
            // Validate
            if (string.IsNullOrEmpty(location)) location = @".\";
            if (string.IsNullOrEmpty(csSolutionPath)) csSolutionPath = @$".\{projectName}.sln";
            if (string.IsNullOrEmpty(csProjName)) csProjName = projectName;

            if (Path.GetFileNameWithoutExtension(location) != projectName)
            {
                location = Path.Combine(location, projectName);
            }

            // Create
            var project = new Project(location, new ProjectDTO()
            {
                ProjectName = projectName,
                CSSolutionPath = csSolutionPath,
                CSProjName = csProjName
            }
            );

            if (!Directory.Exists(project.Location))
            {
                Directory.CreateDirectory(project.Location);
            }

            await CreateSolution(project);
            progress(5);
            await CreateProject(project);
            progress(10);

            // Prepare
            ProjectModel.InitAndSaveCode(project, new ProgramCode());

            var modulesToAdd = new List<string>() { "TypeOCore", "TypeDCore", "TypeODesktop", "TypeOBasic2d", "TypeOSDL", "TypeDSDL" };
            var moduleList = await ModuleProvider.List(project);

            progress(15);

            var moduleAddProgressStep = 15 / modulesToAdd.Count == 0 ? 1 : modulesToAdd.Count;
            var moduleAddProgress = 0;
            foreach(var moduleToAdd in modulesToAdd)
            {
                var addModuleVersion = moduleList.LastOrDefault(m => { return m.Name == moduleToAdd; })?.Versions[0];

                var module = ModuleProvider.Create(moduleToAdd, addModuleVersion.Version);
                ProjectModel.AddModule(project, module);
                moduleAddProgress += moduleAddProgressStep;
                progress(15 + moduleAddProgress);
            }

            progress(30);
            //Save and load the module first so we can call the project creation hook
            await SaveModel.Save(project);
            progress(40);
            
            project = await Load(project.ProjectFilePath, (loadProgress) =>
            {
                progress(40 + (int)(35 * (loadProgress / 100f)));
            });
            
            progress(75);

            HookModel.Shoot(new ProjectCreateHook(project));
            progress(80);

            await SaveModel.Save(project);
            progress(90);
            await ProjectModel.Build(project);
            progress(100);
            // Return
            return project;
        }

        public async Task<Project> Load(string projectFilePath, Action<int> progress)
        {
            if (!projectFilePath.EndsWith(".typeo")) return null;

            progress(0);
            var projectData = JSON.Deserialize<ProjectDTO>(projectFilePath);
            var project = new Project(Path.GetDirectoryName(projectFilePath), projectData);

            var downloadProgressStep = 99 / (project.Modules.Count == 0 ? 1 : project.Modules.Count);
            var downloadProgress = 0;
            // Prepare
            foreach (var module in project.Modules)
            {
                await ModuleModel.Download(module, (bytes, mProgress, totalBytes) => {
                    progress(downloadProgress + (int)(downloadProgressStep * (mProgress / 100f)));
                });
                ModuleModel.LoadAssembly(project, module);
                downloadProgress += downloadProgressStep;
                progress(downloadProgress);
            }

            ProjectModel.LoadAssembly(project);
            ProjectModel.BuildComponentTree(project);

            await RestoreModel.Restore(project);

            progress(100);
            return project;
        }

        public async Task Save(Project project)
        {
           await Task.Run(() =>
            {
                JSON.Serialize(new ProjectDTO()
                {
                    ProjectName = project.ProjectName,
                    CSSolutionPath = project.CSSolutionPath,
                    CSProjName = project.CSProjName,
                    Modules = project.Modules.Select(m => new ModuleDTO() { Name = m.Name, Version = m.Version }).ToList(),
                    StartScene = project.StartScene
                }, project.ProjectFilePath);
            });
        }

        // Internal
        private async Task CreateSolution(Project project)
        {
            if (!File.Exists(Path.Combine(project.Location, project.CSSolutionPath)))
            {
                await CMD.Run(new string[] {
                    $"cd \"{project.Location}\"",
                    $"dotnet new sln --name \"{Path.GetFileNameWithoutExtension(project.CSSolutionPath)}\""
                });
            }
        }

        private async Task CreateProject(Project project)
        {
            if (!File.Exists(project.ProjectCSProjPath))
            {
                await CMD.Run(new string[]
                {
                    $"cd \"{project.Location}\"",
                    $"dotnet new console -lang \"C#\" -n \"{project.CSProjName}\"",
                    $"dotnet sln \"{Path.GetFileName(project.CSSolutionPath)}\" add \"{project.CSProjName}\""
                });

                var csproj = XElement.Load(project.ProjectCSProjPath);
                csproj.Add(
                    new XElement("ItemGroup",
                        new XElement("Compile", new XAttribute("Include", @$"..\TypeO\code\{project.ProjectName}\**\*.typed.cs"),
                            new XElement("Link", "%(RecursiveDir)%(Filename)%(Extension)"),
                            new XElement("Visible", false)
                        )
                    )
                );
                csproj.Save(project.ProjectCSProjPath);
            }
        }
    }
}
