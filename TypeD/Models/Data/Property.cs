using System;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Represents a property with name, description, value, and type information.
    /// </summary>
    public class Property
    {
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
        /// Gets or sets the type of the property value.
        /// </summary>
        public Type Type { get; set; }
        /// <summary>
        /// The component this property belongs to. This reference allows the property to access its parent component, enabling interactions and updates based on the component's state and behavior.
        /// </summary>
        public Component FromComponent { get; set; }
    }
}