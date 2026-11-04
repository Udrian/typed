namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the list of properties of a component has changed, e.g. when the user adds a custom property,
    /// so the properties panel can rebuild its content.
    /// </summary>
    public class ComponentPropertiesChangedHook : Hook
    {
        /// <summary>Gets or sets the component whose property list changed.</summary>
        public Component Component { get; set; }
    }
}
