using TypeD.Helpers;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Provides editor dialogs without coupling modules to concrete views.</summary>
    public interface IEditorDialogModel
    {
        /// <summary>Selects a component matching the supplied filters, or returns null on cancellation.</summary>
        Task<Component> SelectComponentAsync(Project project, FilterHelper typeFilter = null, FilterHelper nameFilter = null, bool excludeAbstract = false);
        /// <summary>Selects a destination for a node move, or returns null on cancellation.</summary>
        Task<Component> SelectMoveTargetAsync(Component root, Component node);
        /// <summary>Selects an icon, or returns null on cancellation.</summary>
        Task<string> SelectIconAsync(Project project, string currentIcon);
        /// <summary>Selects a content path, or returns null on cancellation.</summary>
        Task<string> SelectContentAsync(Project project, IEnumerable<string> extensions);
        /// <summary>Returns a new component name, or null on cancellation.</summary>
        Task<string> RenameComponentAsync(Component component);
        /// <summary>Collects values for a component derived from the specified base component.</summary>
        Task<ComponentCreationOptions> CreateComponentAsync(Project project, string componentNamespace, Component baseComponent);
        /// <summary>Collects a custom property definition and its runtime value type.</summary>
        Task<(CustomProperty Property, Type Type)?> AddCustomPropertyAsync(IEnumerable<IPropertyDefinition> definitions, IEnumerable<string> takenNames);
    }
}