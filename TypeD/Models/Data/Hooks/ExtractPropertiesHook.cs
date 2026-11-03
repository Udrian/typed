
namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the properties of a component are extracted, so listeners can add their own properties.
    /// </summary>
    public class ExtractPropertiesHook : Hook
    {
        /// <summary>Gets or sets the properties collected for the component.</summary>
        public List<Property> Properties { get; set; }
        /// <summary>Gets the component the properties are extracted for.</summary>
        public Component Component { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ExtractPropertiesHook"/> class.</summary>
        public ExtractPropertiesHook() { }

        /// <summary>Initializes a new instance of the <see cref="ExtractPropertiesHook"/> class with an empty property list.</summary>
        /// <param name="component">The component to extract properties for.</param>
        public ExtractPropertiesHook(Component component)
        {
            Properties = new List<Property>();
            Component = component;
        }
    }
}
