using System.Text.Json.Serialization;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Represents a property with name, description, value, and type information.
    /// </summary>
    public class Property
    {
        /// <summary>Initializes a new instance of the <see cref="Property"/> class.</summary>
        public Property() { }

        /// <summary>Initializes a new instance of the <see cref="Property"/> class as a copy of another property.</summary>
        /// <param name="property">The property to copy.</param>
        public Property(Property property)
        {
            Name = property.Name;
            Description = property.Description;
            Group = property.Group;
            DeclaringTypeName = property.DeclaringTypeName;
            Value = property.Value;
            Type = property.Type;
            PropertyTemplateType = property.PropertyTemplateType;
            FromComponent = property.FromComponent;
            ReadOnly = property.ReadOnly;
            DynamicOptions = property.DynamicOptions;
            GenerateCode = property.GenerateCode;
            Hidden = property.Hidden;
        }

        /// <summary>
        /// Creates a string property edited with a combo box whose options are computed from the owning component
        /// each time the editor is built.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="options">Computes the available options from the owning component.</param>
        /// <param name="generateCode">Whether the code generator writes an assignment for the property.</param>
        public static Property CreateDynamicComboBox(string name, Func<Component, IEnumerable<string>> options, bool generateCode = false)
        {
            return new Property
            {
                Name = name,
                Type = typeof(string),
                PropertyTemplateType = typeof(DynamicComboBoxPropertyTemplate),
                DynamicOptions = options,
                GenerateCode = generateCode
            };
        }

        /// <summary>Gets or sets a function computing the selectable options from the owning component; null for a static property.</summary>
        [JsonIgnore]
        public Func<Component, IEnumerable<string>> DynamicOptions { get; set; }
        /// <summary>Gets or sets whether the generated code assigns this property; false for editor-only properties.</summary>
        [JsonIgnore]
        public bool GenerateCode { get; set; } = true;
        /// <summary>Gets or sets whether the properties panel hides this property, for values edited by a module's own editor.</summary>
        [JsonIgnore]
        public bool Hidden { get; set; }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the description of the property.
        /// </summary>
        public string Description { get; set; }
        private string _group = "General";
        /// <summary>Gets or sets the editor group, defaulting to General for an empty group.</summary>
        public string Group
        {
            get => _group;
            set => _group = string.IsNullOrWhiteSpace(value) ? "General" : value.Trim();
        }
        /// <summary>Gets or sets the full name of the class that declares this property.</summary>
        public string DeclaringTypeName { get; set; }
        /// <summary>
        /// Gets or sets the value of the property.
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Converts the underlying value to the specified type.
        /// </summary>
        /// <remarks>The conversion uses a direct cast.</remarks>
        /// <typeparam name="T">The type to which to convert the value.</typeparam>
        /// <returns>The value converted to type <typeparamref name="T"/>.</returns>
        public T ToValue<T>(){ return (T)Value; }
        /// <summary>
        /// Gets or sets the type of the property value.
        /// </summary>
        public Type Type { get; set; }
        /// <summary>Gets or sets the editor template type; null uses the registered property definition's value type.</summary>
        [JsonIgnore]
        public Type PropertyTemplateType { get; set; }
        /// <summary>
        /// Gets or sets whether the object is read only and can not have it's value changed.
        /// </summary>
        public bool ReadOnly { get; set; }
        /// <summary>
        /// The component this property belongs to. This reference allows the property to access its parent component, enabling interactions and updates based on the component's state and behavior.
        /// </summary>
        public Component FromComponent { get; set; }
    }
}