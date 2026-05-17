
namespace TypeD.Models.Data.Hooks
{
    public class ExtractPropertiesHook : Hook
    {
        public List<Property> Properties { get; set; }
        public Component Component { get; private set; }

        public ExtractPropertiesHook() { }
        public ExtractPropertiesHook(Component component)
        {
            Properties = new List<Property>();
            Component = component;
        }
    }
}
