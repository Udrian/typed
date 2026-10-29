namespace TypeD.Models.Data.Hooks
{
    public class ComponentRemovedStateChangedHook : Hook
    {
        public Component Parent { get; set; }
        public Component Child { get; set; }
        public ComponentRemovedStateChangedHook() { }
    }
}
