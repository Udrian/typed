using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    /// <summary>Provider of modules available to projects.</summary>
    public interface IModuleProvider : IProvider
    {
        /// <summary>Lists the available modules.</summary>
        /// <param name="project">The project.</param>
        /// <returns>The module lists.</returns>
        public Task<IEnumerable<ModuleList>> List(Project project);
        /// <summary>Creates a module description.</summary>
        /// <param name="name">The module name.</param>
        /// <param name="version">The module version.</param>
        /// <returns>The module.</returns>
        public Module Create(string name, string version);
        /// <summary>Gets the product description of a released module version, including its dependencies.</summary>
        /// <param name="author">The author (GitHub owner) of the module.</param>
        /// <param name="name">The module name.</param>
        /// <param name="version">The module version.</param>
        /// <returns>The product description, or <c>null</c> when it could not be fetched.</returns>
        public Task<ModuleTypeO> Info(string author, string name, string version);
        /// <summary>Reads the product description of a local module folder.</summary>
        /// <param name="path">The folder containing the module's typeo.json.</param>
        /// <returns>The module with its single local version, or <c>null</c> when the folder is not a module.</returns>
        public ModuleList ReadLocal(string path);
    }
}
