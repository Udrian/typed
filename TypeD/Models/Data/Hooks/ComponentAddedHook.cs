namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after a component has been added as a child of another component.
    /// </summary>
    public class ComponentAddedHook : Hook
    {
        /// <summary>Gets or sets the component the child was added to.</summary>
        public Component Parent { get; set; }
        /// <summary>Gets or sets the component that was added.</summary>
        public Component Child { get; set; }
        /// <summary>Gets or sets the index of the child within the parent, or -1 if unspecified.</summary>
        public int Index { get; set; } = -1;
        /// <summary>Initializes a new instance of the <see cref="ComponentAddedHook"/> class.</summary>
        public ComponentAddedHook() { }
    }
}
