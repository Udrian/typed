using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the context menu of a component is about to be shown, so listeners can add menu items.
    /// </summary>
    public class ComponentContextMenuHook : Hook
    {
        /// <summary>Gets or sets the current project.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component currently open in the editor.</summary>
        public Component OpenedComponent { get; set; }
        /// <summary>Gets or sets the component the context menu was opened on.</summary>
        public Component SelectedComponent { get; set; }
        /// <summary>Gets or sets the menu that listeners add their items to.</summary>
        public Menu Menu { get; set; }
    }
}
