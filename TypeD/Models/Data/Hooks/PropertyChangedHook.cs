namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after a property of a component has changed its value.
    /// </summary>
    public class PropertyChangedHook : Hook
    {
        /// <summary>Gets or sets the component the property belongs to.</summary>
        public Component Component { get; set; }
        /// <summary>Gets or sets the property that changed.</summary>
        public Property Property { get; set; }
        /// <summary>Initializes a new instance of the <see cref="PropertyChangedHook"/> class.</summary>
        public PropertyChangedHook() { }
    }
}
