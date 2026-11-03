namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when a component gets focus (is selected) in the component tree.
    /// </summary>
    public class ComponentFocusHook : Hook
    {
        /// <summary>Gets or sets the current project.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component that received focus.</summary>
        public Component Component { get; set; }
        /// <summary>Gets or sets a value indicating whether the focus came from the root of the selection.</summary>
        public bool Root { get; set; } = true;
        /// <summary>
        /// The full multi-selection, with <see cref="Component"/> being the focused one. Null means only <see cref="Component"/> is selected.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Component> Selection { get; set; }
    }
}
