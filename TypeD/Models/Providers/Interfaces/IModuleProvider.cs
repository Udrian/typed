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
    }
}
