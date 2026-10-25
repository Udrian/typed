namespace TypeD.Models.Data.Hooks
{
    public class PropertyChangedHook : Hook
    {
        public Component Component { get; set; }
        public Property Property { get; set; }
        public PropertyChangedHook() { }
    }
}
