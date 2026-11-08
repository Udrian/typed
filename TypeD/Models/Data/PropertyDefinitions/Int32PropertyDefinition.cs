using System.Globalization;
using TypeD.ViewModel;

namespace TypeD.Models.Data.PropertyDefinitions
{
    /// <summary>
    /// Exposes an integer property value to the property editor.
    /// </summary>
    public class Int32PropertyNode : PropertyNode
    {
        /// <summary>
        /// Creates a node for an integer component property.
        /// </summary>
        public Int32PropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed) : base(component, property, value_changed) { }

        /// <summary>
        /// Gets or sets the property value, converting unrecognized values to zero.
        /// </summary>
        public override object Value
        {
            get
            {
                if (base.Value is int intValue)
                    return intValue;
                return 0;
            }
            set
            {
                if (value is int intValue)
                    base.Value = intValue;
                else if (int.TryParse(value?.ToString(), out int parsedValue))
                    base.Value = parsedValue;
            }
        }
    }

    /// <summary>
    /// Defines the editor and code representation for 32-bit integer properties.
    /// </summary>
    public class Int32PropertyDefinition : PropertyDefinition<int, Int32PropertyNode>
    {
        /// <summary>
        /// Gets the integer property editor template URI.
        /// </summary>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/Int32PropertyTemplate.axaml";
        /// <summary>
        /// Gets the integer property editor template key.
        /// </summary>
        protected override string TemplateKey => "Int32PropertyTemplate";

        /// <summary>
        /// Converts an integer value to its culture-independent C# literal.
        /// </summary>
        public override string ToCode(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
