using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD
{
    /// <summary>
    /// Base class for Module Initialization, use this to setup hooks and configuration
    /// </summary>
    public abstract class TypeDModuleInitializer
    {
        public IHookModel Hooks { get; internal set; }
        public IResourceModel Resources { get; internal set; }
        public abstract void Initializer(Project project);
        public abstract void Uninitializer();
    }
}
