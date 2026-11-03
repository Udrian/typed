namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after a component has been removed from its parent.
    /// </summary>
    public class ComponentRemovedHook : Hook
    {
        /// <summary>Gets or sets the component the child was removed from.</summary>
        public Component Parent { get; set; }
        /// <summary>Gets or sets the component that was removed.</summary>
        public Component Child { get; set; }
        /// <summary>Initializes a new instance of the <see cref="ComponentRemovedHook"/> class.</summary>
        public ComponentRemovedHook() { }
    }
}
