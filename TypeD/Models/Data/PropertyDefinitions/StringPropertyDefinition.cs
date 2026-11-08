using TypeD.ViewModel;

namespace TypeD.Models.Data.PropertyDefinitions
{
    /// <summary>
    /// Exposes a string property value to the property editor.
    /// </summary>
    public class StringPropertyNode : PropertyNode
    {
        /// <summary>
        /// Creates a node for a string component property.
        /// </summary>
        public StringPropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed) : base(component, property, value_changed) { }

        /// <summary>
        /// Gets or sets the property value, converting non-string values to text.
        /// </summary>
        public override object Value
        {
            get
            {
                if (base.Value is string stringValue)
                    return stringValue;
                return "";
            }
            set
            {
                if (value is string stringValue)
                    base.Value = stringValue;
                else
                    base.Value = value?.ToString() ?? "";
            }
        }
    }

    /// <summary>
    /// Defines the editor and code representation for string properties.
    /// </summary>
    public class StringPropertyDefinition : PropertyDefinition<string, StringPropertyNode>
    {
        /// <summary>
        /// Gets the string property editor template URI.
        /// </summary>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/StringPropertyTemplate.axaml";
        /// <summary>
        /// Gets the string property editor template key.
        /// </summary>
        protected override string TemplateKey => "StringPropertyTemplate";

        /// <summary>
        /// Converts a string value to an escaped C# string literal.
        /// </summary>
        public override string ToCode(string value)
        {
            var escaped = value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
            return $"\"{escaped}\"";
        }
    }
}
