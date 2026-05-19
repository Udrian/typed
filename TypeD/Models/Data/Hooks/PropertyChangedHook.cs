namespace TypeD.Models.Data.Hooks
{
    public class PropertyChangedHook : Hook
    {
        public Component Component { get; set; }
        public string ID { get; set; }
        public Property Property { get; set; }
        public PropertyChangedHook() { }
    }
}
