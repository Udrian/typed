namespace TypeD.Models.Data.Hooks
{
    public class ComponentRemovedHook : Hook
    {
        public Component Parent { get; set; }
        public Component Child { get; set; }
        public ComponentRemovedHook() { }
    }
}
