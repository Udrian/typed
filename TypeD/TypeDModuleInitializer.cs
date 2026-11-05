using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD
{
    /// <summary>
    /// Base class for Module Initialization, use this to setup hooks and configuration
    /// </summary>
    public abstract class TypeDModuleInitializer
    {
        /// <summary>Gets the hook model used to subscribe to editor hooks.</summary>
        public IHookModel Hooks { get; internal set; }
        /// <summary>Gets the resource model used to get models and providers.</summary>
        public IResourceModel Resources { get; internal set; }
        /// <summary>Gets module-owned project and component format versions and conversions, registered before initialization.</summary>
        /// <remarks>Registrations use the module name as their stable id and are removed automatically on unload.</remarks>
        public virtual IEnumerable<FormatVersionDefinition> FormatVersions => Array.Empty<FormatVersionDefinition>();
        /// <summary>Called when the module is loaded for a project.</summary>
        /// <param name="project">The project.</param>
        public abstract void Initializer(Project project);
        /// <summary>Called when the module is unloaded.</summary>
        public abstract void Uninitializer();
    }
}
