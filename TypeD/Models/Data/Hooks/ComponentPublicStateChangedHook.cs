namespace TypeD.Models.Data.Hooks
{
    public class ComponentPublicStateChangedHook : Hook
    {
        public Component Parent { get; set; }
        public Component Child { get; set; }
        public ComponentPublicStateChangedHook() { }
    }
}
