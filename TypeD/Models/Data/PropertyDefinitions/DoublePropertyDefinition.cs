using System.Globalization;
using TypeD.ViewModel;

namespace TypeD.Models.Data.PropertyDefinitions
{
    /// <summary>
    /// Exposes a double property value to the property editor.
    /// </summary>
    public class DoublePropertyNode : PropertyNode
    {
        /// <summary>
        /// Creates a node for a double component property.
        /// </summary>
        public DoublePropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed) : base(component, property, value_changed) { }

        /// <summary>
        /// Gets or sets the property value, converting unrecognized values to zero.
        /// </summary>
        public override object Value
        {
            get
            {
                if (base.Value is double doubleValue)
                    return doubleValue;
                return 0.0;
            }
            set
            {
                if (value is double doubleValue)
                    base.Value = doubleValue;
                else if (double.TryParse(value?.ToString(), out double parsedValue))
                    base.Value = parsedValue;
            }
        }
    }

    /// <summary>
    /// Defines the editor and code representation for double properties.
    /// </summary>
    public class DoublePropertyDefinition : PropertyDefinition<double, DoublePropertyNode>
    {
        /// <summary>
        /// Gets the double property editor template URI.
        /// </summary>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/DoublePropertyTemplate.axaml";
        /// <summary>
        /// Gets the double property editor template key.
        /// </summary>
        protected override string TemplateKey => "DoublePropertyTemplate";

        /// <summary>
        /// Converts a double to its C# expression.
        /// </summary>
        public override string ToCode(double value) => DoubleToCode(value);

        /// <summary>
        /// Creates a culture independent C# expression for a double.
        /// </summary>
        public static string DoubleToCode(double value)
        {
            if (double.IsNaN(value)) return "double.NaN";
            if (double.IsPositiveInfinity(value)) return "double.PositiveInfinity";
            if (double.IsNegativeInfinity(value)) return "double.NegativeInfinity";
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
