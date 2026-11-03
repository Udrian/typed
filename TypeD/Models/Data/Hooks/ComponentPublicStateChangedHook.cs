namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after the public state of a child component (whether it is exposed to its parent) has changed.
    /// </summary>
    public class ComponentPublicStateChangedHook : Hook
    {
        /// <summary>Gets or sets the parent of the changed component.</summary>
        public Component Parent { get; set; }
        /// <summary>Gets or sets the component whose public state changed.</summary>
        public Component Child { get; set; }
        /// <summary>Initializes a new instance of the <see cref="ComponentPublicStateChangedHook"/> class.</summary>
        public ComponentPublicStateChangedHook() { }
    }
}
