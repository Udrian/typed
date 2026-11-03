namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after the removed state of a child component (whether it is marked as removed) has changed.
    /// </summary>
    public class ComponentRemovedStateChangedHook : Hook
    {
        /// <summary>Gets or sets the parent of the changed component.</summary>
        public Component Parent { get; set; }
        /// <summary>Gets or sets the component whose removed state changed.</summary>
        public Component Child { get; set; }
        /// <summary>Initializes a new instance of the <see cref="ComponentRemovedStateChangedHook"/> class.</summary>
        public ComponentRemovedStateChangedHook() { }
    }
}
