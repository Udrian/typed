using TypeD.ViewModel;

namespace TypeD.Models.Data.PropertyDefinitions
{
    /// <summary>
    /// Exposes an enum property value and its available options to the property editor.
    /// </summary>
    public class EnumPropertyNode : PropertyNode
    {
        /// <summary>
        /// Creates a node for an enum component property.
        /// </summary>
        public EnumPropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed) : base(component, property, value_changed) { }

        /// <summary>
        /// Gets the values of the property's enum type.
        /// </summary>
        public Array Options => Type != null && Type.IsEnum ? Enum.GetValues(Type) : Array.Empty<object>();

        /// <summary>
        /// Gets or sets the selected enum value.
        /// </summary>
        public override object Value
        {
            get => base.Value;
            set
            {
                // The ListBox sends null when the selection is cleared.
                if (value != null && Type.IsInstanceOfType(value))
                    base.Value = value;
            }
        }
    }

    /// <summary>
    /// Handles every enum type, registered under <see cref="Enum"/>.
    /// </summary>
    public class EnumPropertyDefinition : PropertyDefinition<Enum, EnumPropertyNode>
    {
        /// <summary>
        /// Gets the enum property editor template URI.
        /// </summary>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/EnumPropertyTemplate.axaml";
        /// <summary>
        /// Gets the enum property editor template key.
        /// </summary>
        protected override string TemplateKey => "EnumPropertyTemplate";

        /// <summary>
        /// Converts an enum value to a C# expression, including flags values.
        /// </summary>
        public override string ToCode(Enum value)
        {
            var typeName = value.GetType().FullName.Replace('+', '.');
            var names = value.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" | ", names.Select(name =>
                char.IsDigit(name[0]) || name[0] == '-'
                    ? $"({typeName}){name}"
                    : $"{typeName}.{name}"));
        }
    }
}
