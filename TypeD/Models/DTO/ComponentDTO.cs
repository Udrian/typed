using System.Collections.Generic;
using TypeD.Models.Data;

namespace TypeD.Models.DTO
{
    /// <summary>
    /// Represents a data transfer object (DTO) for a <see cref="Component"/>, encapsulating metadata such as class name,
    /// namespace, inheritance, and relationships with other <see cref="Component"/>.
    /// </summary>
    /// <remarks>This class is designed to provide a structured representation of a <see cref="Component"/>'s metadata,
    /// including its class name, namespace, parent <see cref="Component"/>, implemented interfaces, and child <see cref="Component"/>s. It is 
    /// typically used in scenarios where <see cref="Component"/> metadata needs to be serialized, transferred, or
    /// analyzed.</remarks>
    public class ComponentDTO
    {
        /// <summary>
        /// Gets or sets the name of the <see cref="Component"/> class name.
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// Gets or sets the namespace associated with the current <see cref="Component"/>.
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// Gets or sets the name of the template class associated with the current <see cref="Component"/>.
        /// </summary>
        public string TemplateClass { get; set; }
        /// <summary>
        /// Gets or sets the name of the parent <see cref="Component"/> associated with this <see cref="Component"/>.
        /// </summary>
        public string ParentComponent { get; set; }
        /// <summary>
        /// Gets or sets the list of interfaces associated with the current <see cref="Component"/>.
        /// </summary>
        public List<string> Interfaces { get; set; }
        /// <summary>
        /// Gets or sets the base type of the object represented by this <see cref="Component"/>.
        /// </summary>
        public string TypeOBaseType { get; set; }
        /// <summary>
        /// Gets or sets the collection of child names associated with the <see cref="Component"/>.
        /// </summary>
        public List<string> Children { get; set; }
    }
}
