using System.Xml.Linq;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Data.SaveContexts;
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
        IComponentProvider ComponentProvider { get; set; }

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
            ComponentProvider = ResourceModel.Get<IComponentProvider>();
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
            SaveModel.AddSave<ProjectSaveContext>(project);
            ProjectModel.InitAndSaveCode(project, new ProgramCode());

            await ModuleModel.Initialize(project);
            progress(30);
            //Save and load the module first so we can call the project creation hook
            await SaveModel.Save(project);
            progress(40);
            
            project = await Load(project.ProjectFilePath, (loadProgress) =>
            {
                progress(40 + (int)(35 * (loadProgress / 100f)));
            }, false);
            
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

        public async Task<Project> Load(string projectFilePath, Action<int> progress, bool regenerateComponentCode = true, Action<ProgressUpdate> report = null)
        {
            if (!projectFilePath.EndsWith(".typeo")) return null;

            void Report(int percent, string message, string subTask = null, int? subProgress = null)
            {
                progress(percent);
                report?.Invoke(new ProgressUpdate(percent, message, subTask, subProgress));
            }

            Report(0, "Reading project file");
            var projectData = JSON.Deserialize<ProjectDTO>(projectFilePath);
            var project = new Project(Path.GetDirectoryName(projectFilePath), projectData);

            Report(5, "Initializing project");
            ResourceModel.Get<IIconModel>().Sync(project);
            TypeDInit.ProjectLoad(project, ResourceModel);

            // Prepare
            Report(10, "Resolving modules");
            await ModuleModel.Resolve(project, update =>
            {
                // Modules take up 10-70% of the load
                Report(10 + (int)(60 * (update.Progress / 100f)), update.Message, update.SubTask, update.SubProgress);
            });

            Report(70, "Loading assemblies");
            ResourceModel.Get<IIconModel>().Sync(project);

            ProjectModel.LoadAssembly(project);
            Report(78, "Building component tree");
            ProjectModel.BuildComponentTree(project);

            Report(85, "Restoring project");
            await RestoreModel.Restore(project);

            if (regenerateComponentCode)
            {
                Report(90, "Generating component code");
                ProjectModel.StopWatching(project);
                try
                {
                    foreach (var component in ComponentProvider.ListAll(project))
                    {
                        component.Template.Init();
                        ProjectModel.InitAndSaveCode(project, component.Template.Code);
                    }

                    Report(96, "Saving project");
                    await SaveModel.Save(project);
                }
                finally
                {
                    ProjectModel.StartWatching(project);
                }
            }

            Report(100, "Project loaded");
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
                    $"dotnet new console --language \"C#\" --framework \"net7.0\" -n \"{project.CSProjName}\"",
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
