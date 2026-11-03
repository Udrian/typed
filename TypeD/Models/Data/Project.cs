using System.Reflection;
using TypeD.Models.DTO;
using TypeD.View.TreeNodes;

namespace TypeD.Models.Data
{
    /// <summary>
    /// A TypeO project: its configuration, modules, file locations and component tree.
    /// </summary>
    public class Project
    {
        // Data
        /// <summary>Gets the project name.</summary>
        public string ProjectName { get; private set; }
        /// <summary>Gets the path of the C# solution of the project.</summary>
        public string CSSolutionPath { get; private set; }
        /// <summary>Gets the name of the C# project.</summary>
        public string CSProjName { get; private set; }
        /// <summary>Gets the modules the project uses.</summary>
        public List<Module> Modules { get; private set; }
        /// <summary>Gets or sets the scene the game starts with.</summary>
        public string StartScene { get; set; }

        /// <summary>Gets the folder the project is located in.</summary>
        public string Location { get; private set; }

        // Paths
        /// <summary>Gets the path of the project file (.typeo).</summary>
        public string ProjectFilePath { get { return Path.Combine(Location, $@"{ProjectName}.typeo"); } }
        /// <summary>Gets the path of the TypeO working folder of the project.</summary>
        public string ProjectTypeOPath { get { return Path.Combine(Location, "TypeO"); } }
        /// <summary>Gets the path the project is built to.</summary>
        public string ProjectBuildOutputPath { get { return Path.Combine(ProjectTypeOPath, "build", CSProjName); } }
        /// <summary>Gets the path of the generated component code.</summary>
        public string ProjectComponentCodePath { get { return Path.Combine(ProjectTypeOPath, "code"); } }
        /// <summary>Gets the path of the project's source folder.</summary>
        public string ProjectSourcePath { get { return Path.Combine(Location, ProjectName); } }
        /// <summary>Gets the path of the project's csproj file.</summary>
        public string ProjectCSProjPath { get { return Path.Combine(Location, ProjectName, $"{ProjectName}.csproj"); } }
        /// <summary>Gets the path of the project's content folder.</summary>
        public string ProjectContentPath { get { return Path.Combine(Location, "content"); } }
        /// <summary>Gets the path of the editor icons folder.</summary>
        public string ProjectIconsPath { get { return Path.Combine(ProjectTypeOPath, "editor", "icons"); } }

        // Tree
        /// <summary>Gets or sets the tree of the project's components.</summary>
        public Tree ComponentTree { get; set; }

        // Loaded data
        /// <summary>Gets the compiled project assembly, once loaded.</summary>
        public Assembly Assembly { get; internal set; }
        /// <summary>Gets or sets a value indicating whether the project is being closed.</summary>
        public bool IsClosing { get; set; }

        // Constructor
        internal Project(string location, ProjectDTO projectData)
        {
            Location = location;

            ComponentTree = new Tree();

            ProjectName = projectData.ProjectName;
            CSSolutionPath = projectData.CSSolutionPath;
            CSProjName = projectData.CSProjName;
            Modules = projectData.Modules.Select(m => new Module() { Name = m.Name, Version = m.Version }).ToList();
            StartScene = projectData.StartScene;
        }
    }
}
