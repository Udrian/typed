using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that runs typer to list, resolve, fetch and update modules and dependencies.</summary>
    public interface ITyperModel : IModel
    {
        /// <summary>Gets a value indicating whether typer could be found.</summary>
        public bool IsAvailable { get; }
        /// <summary>Lists all registered modules with their versions.</summary>
        public Task<IEnumerable<ModuleList>> ListModules();
        /// <summary>Lists the effective dependencies of a project or local module.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        public Task<IEnumerable<ModuleDependency>> ListDependencies(string projectPath);
        /// <summary>Gets the product description of a released module version, from the module cache or GitHub.</summary>
        /// <param name="author">The author (GitHub owner) of the module.</param>
        /// <param name="name">The module name.</param>
        /// <param name="version">The module version.</param>
        /// <returns>The product description, or <c>null</c> when it could not be fetched.</returns>
        public Task<ModuleTypeO> GetModuleTypeO(string author, string name, string version);
        /// <summary>Adds or replaces a dependency in typeo.json.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="dependency">Dependency string, for example <c>Udrian@TypeOCore-0.1.0</c>.</param>
        public Task<bool> AddDependency(string projectPath, string dependency);
        /// <summary>Removes a dependency from typeo.json.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="name">Name of the dependency.</param>
        public Task<bool> RemoveDependency(string projectPath, string name);
        /// <summary>Updates one or all dependencies to the latest or a given version.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="name">Name of the dependency, or <c>null</c> for all.</param>
        /// <param name="version">Version to update to, or <c>null</c> for the latest release.</param>
        public Task<bool> UpdateDependency(string projectPath, string name = null, string version = null);
        /// <summary>Overrides a dependency with a local module in typeo_override.json.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="name">Name of the dependency.</param>
        /// <param name="localPath">Folder of the local module.</param>
        public Task<bool> AddOverride(string projectPath, string name, string localPath);
        /// <summary>Removes a dependency override.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="name">Name of the dependency.</param>
        public Task<bool> RemoveOverride(string projectPath, string name);
        /// <summary>Fetches missing dependencies and updates the solution and project files.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="onOutput">Called with each line typer reports while resolving, for example git clone progress.</param>
        public Task<bool> Resolve(string projectPath, Action<string> onOutput = null);
        /// <summary>Updates dependencies throughout the module graph using the project's resolved module versions.</summary>
        /// <param name="projectPath">Folder containing typeo.json.</param>
        /// <param name="resolvedModulesPath">Project TypeO/cache/modules.json, or <c>null</c> for normal dependency resolution.</param>
        /// <param name="onOutput">Called with each line typer reports while resolving.</param>
        public Task<bool> Resolve(string projectPath, string resolvedModulesPath, Action<string> onOutput);
        /// <summary>Creates a new module project.</summary>
        /// <param name="name">Name of the module.</param>
        /// <param name="outputPath">Folder to create the module folder in.</param>
        /// <param name="dependencies">Dependency strings of the new module, defaults to TypeOCore.</param>
        public Task<bool> CreateModule(string name, string outputPath, IEnumerable<string> dependencies = null);
    }
}
