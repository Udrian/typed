using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    /// <summary>Provider that creates, loads and saves projects.</summary>
    public interface IProjectProvider : IProvider
    {
        /// <summary>Creates a new project.</summary>
        /// <param name="projectName">The project name.</param>
        /// <param name="location">The folder to create it in.</param>
        /// <param name="csSolutionPath">The C# solution path.</param>
        /// <param name="csProjName">The C# project name.</param>
        /// <param name="modulesToAdd">Optional collection of (Author, Name, Version, LocalPath) tuples for modules to add to the project; a non-empty LocalPath adds the module from that local folder.</param>
        /// <param name="progress">Callback reporting progress percentage.</param>
        /// <param name="report">Callback reporting what is being done, including subtasks.</param>
        /// <returns>The created project.</returns>
        public Task<Project> Create(string projectName, string location, string csSolutionPath, string csProjName, ObservableCollection<(string Author, string Name, string Version, string LocalPath)> modulesToAdd = null, Action<int> progress = null, Action<ProgressUpdate> report = null);
        /// <summary>
        /// Reads project configuration without initializing the project or loading modules.
        /// Module-model changes on this instance only update dependency configuration; resolving,
        /// building, and loading are deferred until the project is opened.
        /// </summary>
        /// <param name="projectFilePath">The project file path.</param>
        /// <returns>The configuration-only project.</returns>
        public Project ReadConfiguration(string projectFilePath);
        /// <summary>Loads a project from its file.</summary>
        /// <param name="projectFilePath">The project file path.</param>
        /// <param name="progress">Callback reporting progress.</param>
        /// <param name="regenerateComponentCode">Whether component code is regenerated.</param>
        /// <param name="report">Callback reporting what is being done, including subtasks like fetching and building modules.</param>
        /// <returns>The loaded project.</returns>
        public Task<Project> Load(string projectFilePath, Action<int> progress, bool regenerateComponentCode = true, Action<ProgressUpdate> report = null);
        /// <summary>Saves a project.</summary>
        /// <param name="project">The project.</param>
        public Task Save(Project project);
    }
}

