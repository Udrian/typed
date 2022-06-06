using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TypeD.Models.DTO;
using TypeD.View.TreeNodes;

namespace TypeD.Models.Data
{
    public class Project
    {
        // Data
        public string ProjectName { get; private set; }
        public string CSSolutionPath { get; private set; }
        public string CSProjName { get; private set; }
        public List<Module> Modules { get; private set; }
        public string StartScene { get; set; }

        public string Location { get; private set; }

        // Paths
        public string ProjectFilePath { get { return Path.Combine(Location, $@"{ProjectName}.typeo"); } }
        public string ProjectTypeOPath { get { return Path.Combine(Location, "TypeO"); } }
        public string ProjectBuildOutputPath { get { return Path.Combine(ProjectTypeOPath, "build", CSProjName); } }
        public string ProjectComponentCodePath { get { return Path.Combine(ProjectTypeOPath, "code"); } }
        public string ProjectSourcePath { get { return Path.Combine(Location, ProjectName); } }
        public string ProjectCSProjPath { get { return Path.Combine(Location, ProjectName, $"{ProjectName}.csproj"); } }

        // Tree
        public Tree ComponentTree { get; set; }

        // Loaded data
        public Assembly Assembly { get; internal set; }
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
