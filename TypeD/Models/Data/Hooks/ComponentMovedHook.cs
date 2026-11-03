namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after a component has been moved to a new position or parent.
    /// </summary>
    public class ComponentMovedHook : Hook
    {
        /// <summary>Gets or sets the parent the child was moved to.</summary>
        public Component Parent { get; set; }
        /// <summary>Gets or sets the component that was moved.</summary>
        public Component Child { get; set; }
        /// <summary>Gets or sets the new index of the child within the parent.</summary>
        public int Index { get; set; }
        /// <summary>Initializes a new instance of the <see cref="ComponentMovedHook"/> class.</summary>
        public ComponentMovedHook() { }
    }
}
