using System;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that manages, builds, loads and unloads the modules of a project through typer.</summary>
    public interface IModuleModel : IModel
    {
        /// <summary>Creates typeo.json for a project that does not have one yet, using its current modules.</summary>
        /// <param name="project">The project.</param>
        public Task Initialize(Project project);
        /// <summary>Resolves the project's dependencies with typer, then builds and loads the resulting modules. Does not save the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="report">Called with the progress of the resolve, including the git fetches and module builds it runs.</param>
        /// <returns><c>true</c> if typer resolved the dependencies.</returns>
        public Task<bool> Resolve(Project project, Action<ProgressUpdate> report = null);
        /// <summary>Adds a module to the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="author">The author (GitHub owner) of the module.</param>
        /// <param name="name">The module name.</param>
        /// <param name="version">The module version.</param>
        public Task<bool> Add(Project project, string author, string name, string version);
        /// <summary>Removes a module from the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="name">The module name.</param>
        public Task<bool> Remove(Project project, string name);
        /// <summary>Updates a module, or all modules, to the latest or a given version.</summary>
        /// <param name="project">The project.</param>
        /// <param name="name">The module name, or <c>null</c> for all modules.</param>
        /// <param name="version">The version, or <c>null</c> for the latest release.</param>
        public Task<bool> Update(Project project, string name = null, string version = null);
        /// <summary>Uses a local module instead of the released one, for local module development.</summary>
        /// <param name="project">The project.</param>
        /// <param name="name">The module name.</param>
        /// <param name="path">The folder of the local module.</param>
        public Task<bool> UseLocal(Project project, string name, string path);
        /// <summary>Stops using a local module and goes back to the released version.</summary>
        /// <param name="project">The project.</param>
        /// <param name="name">The module name.</param>
        public Task<bool> UseReleased(Project project, string name);
        /// <summary>Updates the dependencies of a local module that is used by the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="name">The name of the local module.</param>
        public Task<bool> UpdateLocal(Project project, string name);
        /// <summary>Creates a new module with typer and uses it as a local module in the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="name">The name of the new module.</param>
        /// <param name="outputPath">The folder to create the module folder in.</param>
        public Task<bool> CreateLocal(Project project, string name, string outputPath);
        /// <summary>Builds a module so its assemblies can be loaded.</summary>
        /// <param name="module">The module to build.</param>
        /// <returns><c>true</c> if the module is built.</returns>
        public Task<bool> Build(Module module);
        /// <summary>Loads the assemblies of a module for a project.</summary>
        /// <param name="project">The project the module is used by.</param>
        /// <param name="module">The module to load.</param>
        public void LoadAssembly(Project project, Module module);
        /// <summary>Unloads the assemblies of a module.</summary>
        /// <param name="module">The module to unload.</param>
        public void UnloadAssembly(Module module);
    }
}
