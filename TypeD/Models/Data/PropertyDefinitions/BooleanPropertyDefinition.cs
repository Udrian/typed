using TypeD.ViewModel;

namespace TypeD.Models.Data.PropertyDefinitions
{
    /// <summary>
    /// Exposes a boolean property value to the property editor.
    /// </summary>
    public class BooleanPropertyNode : PropertyNode
    {
        /// <summary>
        /// Creates a node for a boolean component property.
        /// </summary>
        public BooleanPropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed) : base(component, property, value_changed) { }

        /// <summary>
        /// Gets or sets the property value, converting unrecognized values to false.
        /// </summary>
        public override object Value
        {
            get
            {
                if (base.Value is bool boolValue)
                    return boolValue;
                return false;
            }
            set
            {
                if (value is bool boolValue)
                    base.Value = boolValue;
                else if (bool.TryParse(value?.ToString(), out bool parsedValue))
                    base.Value = parsedValue;
            }
        }
    }

    /// <summary>
    /// Defines the editor and code representation for boolean properties.
    /// </summary>
    public class BooleanPropertyDefinition : PropertyDefinition<bool, BooleanPropertyNode>
    {
        /// <summary>
        /// Gets the Boolean property editor template URI.
        /// </summary>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/BooleanPropertyTemplate.axaml";
        /// <summary>
        /// Gets the Boolean property editor template key.
        /// </summary>
        protected override string TemplateKey => "BooleanPropertyTemplate";

        /// <summary>
        /// Converts a boolean value to its C# literal.
        /// </summary>
        public override string ToCode(bool value) => value ? "true" : "false";
    }
}
