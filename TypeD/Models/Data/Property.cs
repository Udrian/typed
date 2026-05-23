using System;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Represents a property with name, description, value, and type information.
    /// </summary>
    public class Property
    {
        public Property() { }

        public Property(Property property)
        {
            Name = property.Name;
            Description = property.Description;
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