namespace TypeD.Models.Data
{
    /// <summary>
    /// The C# access modifier of a user defined property.
    /// </summary>
    public enum CustomPropertyAccess
    {
        /// <summary>The property is generated as <c>public</c>.</summary>
        Public,
        /// <summary>The property is generated as <c>private</c>.</summary>
        Private,
        /// <summary>The property is generated as <c>protected</c>.</summary>
        Protected
    }

    /// <summary>
    /// A property the user has added to a component. It is stored in the component file and generated into the
    /// component's code as a C# property marked with the TypeOProperty attribute.
    /// </summary>
    public class CustomProperty
    {
        /// <summary>Initializes a new instance of the <see cref="CustomProperty"/> class.</summary>
        public CustomProperty() { }

        /// <summary>Initializes a new instance of the <see cref="CustomProperty"/> class as a copy of another custom property.</summary>
        /// <param name="customProperty">The custom property to copy.</param>
        public CustomProperty(CustomProperty customProperty)
        {
            Name = customProperty.Name;
            Description = customProperty.Description;
            TypeName = customProperty.TypeName;
            DefaultValue = customProperty.DefaultValue;
            Group = customProperty.Group;
            Access = customProperty.Access;
        }

        /// <summary>Gets or sets the name of the generated C# property.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the description passed to the TypeOProperty attribute, or null for none.</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the full name of the property value type.</summary>
        public string TypeName { get; set; }

        /// <summary>Gets or sets the default value assigned to the generated property.</summary>
        public object DefaultValue { get; set; }

        private string _group = "General";
        /// <summary>Gets or sets the editor group, defaulting to General for an empty group.</summary>
        public string Group
        {
            get => _group;
            set => _group = string.IsNullOrWhiteSpace(value) ? "General" : value.Trim();
        }

        /// <summary>Gets or sets the access modifier of the generated property.</summary>
        public CustomPropertyAccess Access { get; set; }
    }
}
