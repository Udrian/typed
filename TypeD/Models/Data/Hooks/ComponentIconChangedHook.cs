namespace TypeD.Models.Data.Hooks
{
    public class ComponentIconChangedHook : Hook
    {
        public Project Project { get; set; }
        public Component Component { get; set; }
        public ComponentIconChangedHook() { }
    }
}
