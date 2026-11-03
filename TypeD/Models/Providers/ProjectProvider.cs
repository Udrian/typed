using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
        public async Task<Project> Create(string projectName, string location, string csSolutionPath, string csProjName, ObservableCollection<(string Author, string Name, string Version)> modulesToAdd = null, Action<int> progress = null, Action<ProgressUpdate> report = null)
        {
            progress = progress ?? (_ => { });
            report = report ?? (_ => { });
            progress(0);
            report(new ProgressUpdate(0, "Validating project settings"));
            
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

            report(new ProgressUpdate(1, "Creating project directory"));
            if (!Directory.Exists(project.Location))
            {
                Directory.CreateDirectory(project.Location);
            }

            report(new ProgressUpdate(3, "Creating solution file"));
            await CreateSolution(project);
            progress(5);
            
            report(new ProgressUpdate(5, "Creating C# project"));
            await CreateProject(project);
            progress(10);

            // Prepare
            report(new ProgressUpdate(10, "Initializing project structure"));
            SaveModel.AddSave<ProjectSaveContext>(project);
            ProjectModel.InitAndSaveCode(project, new ProgramCode());

            report(new ProgressUpdate(15, "Initializing modules"));
            await ModuleModel.Initialize(project);
            progress(30);
            
            report(new ProgressUpdate(30, "Saving project"));
            //Save and load the module first so we can call the project creation hook
            await SaveModel.Save(project);
            progress(40);
            
            report(new ProgressUpdate(40, "Loading project"));
            project = await Load(project.ProjectFilePath, (loadProgress) =>
            {
                progress(40 + (int)(35 * (loadProgress / 100f)));
            }, false, update =>
            {
                report(new ProgressUpdate(40 + (int)(35 * (update.Progress / 100f)), "Loading project", update.SubTask, update.SubProgress));
            });
            
            progress(75);

            // Add selected modules if provided
            if (modulesToAdd != null && modulesToAdd.Count > 0)
            {
                report(new ProgressUpdate(75, $"Adding {modulesToAdd.Count} modules"));
                int moduleIndex = 0;
                foreach (var module in modulesToAdd)
                {
                    moduleIndex++;
                    report(new ProgressUpdate(75, $"Adding modules", $"{module.Name} ({moduleIndex}/{modulesToAdd.Count})", moduleIndex * 100 / modulesToAdd.Count));
                    try
                    {
                        await ModuleModel.Add(project, module.Author, module.Name, module.Version);
                    }
                    catch
                    {
                        // Continue if a module fails to add
                    }
                }
                
                // Reload the project to include the added modules
                report(new ProgressUpdate(90, "Saving project with modules"));
                await SaveModel.Save(project);
                
                report(new ProgressUpdate(95, "Reloading project"));
                project = await Load(project.ProjectFilePath, _ => { }, false);
            }

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
        /// <summary>
        /// Finds the actual solution file, checking for both .sln and .slnx files.
        /// Returns the path to whichever exists, or null if neither exists.
        /// </summary>
        private string GetSolutionFilePath(Project project)
        {
            var basePath = Path.Combine(project.Location, Path.GetFileNameWithoutExtension(project.CSSolutionPath));
            var slnPath = basePath + ".sln";
            var slnxPath = basePath + ".slnx";
            
            if (File.Exists(slnPath))
                return slnPath;
            if (File.Exists(slnxPath))
                return slnxPath;
            
            return null;
        }

        private async Task CreateSolution(Project project)
        {
            var basePath = Path.Combine(project.Location, Path.GetFileNameWithoutExtension(project.CSSolutionPath));
            var slnPath = basePath + ".sln";
            var slnxPath = basePath + ".slnx";
            
            CMD.ReportOutput($"[ProjectCreation] Creating solution at: {basePath}");
            
            var existingSolution = GetSolutionFilePath(project);
            if (existingSolution == null)
            {
                CMD.ReportOutput($"[ProjectCreation] Solution file does not exist, running dotnet new sln");
                await CMD.Run(new string[] {
                    $"cd \"{project.Location}\"",
                    $"dotnet new sln --name \"{Path.GetFileNameWithoutExtension(project.CSSolutionPath)}\""
                });
                
                // Wait a bit for file system to sync
                await Task.Delay(500);
                
                existingSolution = GetSolutionFilePath(project);
                if (existingSolution == null)
                {
                    CMD.ReportOutput($"[ProjectCreation] ERROR: Solution file was not created (checked for .sln and .slnx)");
                }
                else
                {
                    var fileType = Path.GetExtension(existingSolution);
                    CMD.ReportOutput($"[ProjectCreation] Solution file created successfully ({fileType})");
                }
            }
            else
            {
                var fileType = Path.GetExtension(existingSolution);
                CMD.ReportOutput($"[ProjectCreation] Solution file already exists ({fileType})");
            }
        }

        private async Task CreateProject(Project project)
        {
            if (!File.Exists(project.ProjectCSProjPath))
            {
                CMD.ReportOutput($"[ProjectCreation] Creating project at: {project.Location}");
                CMD.ReportOutput($"[ProjectCreation] Project name: {project.ProjectName}");
                CMD.ReportOutput($"[ProjectCreation] CS Project name: {project.CSProjName}");
                CMD.ReportOutput($"[ProjectCreation] Expected .csproj path: {project.ProjectCSProjPath}");

                // Verify solution file exists before continuing
                var solutionFilePath = GetSolutionFilePath(project);
                if (solutionFilePath == null)
                {
                    CMD.ReportOutput($"[ProjectCreation] ERROR: Solution file not found");
                    CMD.ReportOutput($"[ProjectCreation] Attempting to create it again...");
                    await CreateSolution(project);
                    
                    // Verify it was created
                    solutionFilePath = GetSolutionFilePath(project);
                    if (solutionFilePath == null)
                    {
                        throw new FileNotFoundException($"Solution file was not created. Please check that 'dotnet new sln' command works in your environment.");
                    }
                }

                // Create the project subdirectory if it doesn't exist
                var projectDir = Path.Combine(project.Location, project.CSProjName);
                CMD.ReportOutput($"[ProjectCreation] Creating directory: {projectDir}");
                
                if (!Directory.Exists(projectDir))
                {
                    Directory.CreateDirectory(projectDir);
                    CMD.ReportOutput($"[ProjectCreation] Directory created");
                }
                else
                {
                    CMD.ReportOutput($"[ProjectCreation] Directory already exists");
                }

                CMD.ReportOutput($"[ProjectCreation] Running dotnet new console (.NET 10.0) in: {projectDir}");
                
                // Run commands one at a time for better error visibility
                // Using .NET 10.0 instead of 7.0 since it's the latest stable version
                await CMD.Run(new string[]
                {
                    $"cd \"{projectDir}\"",
                    $"dotnet new console --language \"C#\" --framework \"net10.0\""
                });

                CMD.ReportOutput($"[ProjectCreation] dotnet new console completed");

                // Wait a bit longer for file system to sync (important for OneDrive)
                await Task.Delay(500);
                
                // Check if the project file was created
                if (!File.Exists(project.ProjectCSProjPath))
                {
                    CMD.ReportOutput($"[ProjectCreation] ERROR: dotnet new console did not create .csproj file!");
                    CMD.ReportOutput($"[ProjectCreation] Listing directory contents:");
                    try
                    {
                        var files = Directory.GetFiles(projectDir);
                        if (files.Length == 0)
                        {
                            CMD.ReportOutput($"[ProjectCreation] Directory is empty!");
                        }
                        foreach (var file in files)
                        {
                            CMD.ReportOutput($"[ProjectCreation]   - {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        CMD.ReportOutput($"[ProjectCreation]   Error listing files: {ex.Message}");
                    }
                    
                    throw new FileNotFoundException($"Project file was not created at {project.ProjectCSProjPath}. The 'dotnet new console' command may have failed. Check that .NET SDK 10.0 is properly installed and the path doesn't contain special characters or is not under OneDrive sync.");
                }

                CMD.ReportOutput($"[ProjectCreation] .csproj file found, adding to solution");

                // Verify solution file still exists before adding project
                solutionFilePath = GetSolutionFilePath(project);
                if (solutionFilePath == null)
                {
                    throw new FileNotFoundException($"Solution file disappeared");
                }

                // Add the project to the solution
                // Use the actual solution filename (whether it's .sln or .slnx)
                var solutionFileName = Path.GetFileName(solutionFilePath);
                await CMD.Run(new string[]
                {
                    $"cd \"{project.Location}\"",
                    $"dotnet sln \"{solutionFileName}\" add \"{Path.Combine(project.CSProjName, project.CSProjName + ".csproj")}\""
                });

                CMD.ReportOutput($"[ProjectCreation] Project added to solution");

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
                
                CMD.ReportOutput($"[ProjectCreation] Project creation completed successfully");
            }
        }
    }
}
