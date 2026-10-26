namespace TypeD.Models.DTO
{
    /// <summary>
    /// Represents a Component child together with it's overridden properties and added children.
    /// </summary>
    public class ComponentChildDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of this child component instance.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the fully resolved Component type reflection of the child component.
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// Gets or sets the collection of overridden properties associated with the child component.
        /// </summary>
        public List<PropertyDTO> Properties { get; set; }
        /// <summary>
        /// Gets or sets child instances added to this component instance.
        /// </summary>
        public List<ComponentChildDTO> Children { get; set; }

    }
}
