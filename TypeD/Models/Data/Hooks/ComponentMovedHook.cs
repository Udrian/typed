namespace TypeD.Models.Data.Hooks
{
    public class ComponentMovedHook : Hook
    {
        public Component Parent { get; set; }
        public Component Child { get; set; }
        public int Index { get; set; }
        public ComponentMovedHook() { }
    }
}
