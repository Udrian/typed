namespace TypeD.Models.Data.Hooks
{
    public class ComponentAddedHook : Hook
    {
        public Component Parent { get; set; }
        public Component Child { get; set; }
        public ComponentAddedHook() { }
    }
}
