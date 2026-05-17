namespace TypeD.Models.DTO
{
    /// <summary>
    /// Represents a data transfer object that describes a child component and its associated properties.
    /// </summary>
    /// <remarks>Use this class to transfer information about a component's child, including its full name and
    /// a collection of its properties, typically in serialization or API scenarios.</remarks>
    public class ComponentChildDTO
    {
        /// <summary>
        /// Gets or sets the fully resolved Component type reflection of the child component.
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// Gets or sets the collection of overridden properties associated with the child component.
        /// </summary>
        public List<PropertyDTO> Properties { get; set; }

    }
}
