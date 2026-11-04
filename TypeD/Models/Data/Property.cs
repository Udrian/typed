using System;

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
            FromComponent = property.FromComponent;
            ReadOnly = property.ReadOnly;
        }

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