using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>Allows modules to add actions to a property panel context menu.</summary>
    public class PropertyContextMenuHook : Hook
    {
        /// <summary>Gets or sets the project containing the property.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component whose property is displayed.</summary>
        public Component Component { get; set; }
        /// <summary>Gets or sets the property whose context menu is being opened.</summary>
        public Property Property { get; set; }
        /// <summary>Gets the menu to which actions can be added.</summary>
        public Menu Menu { get; } = new Menu();
    }
}
