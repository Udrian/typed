namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after the icon of a component has changed.
    /// </summary>
    public class ComponentIconChangedHook : Hook
    {
        /// <summary>Gets or sets the current project.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component whose icon changed.</summary>
        public Component Component { get; set; }
        /// <summary>Initializes a new instance of the <see cref="ComponentIconChangedHook"/> class.</summary>
        public ComponentIconChangedHook() { }
    }
}
