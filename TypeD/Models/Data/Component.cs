using System;
using System.Collections.Generic;
using TypeD.Components;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Represents a component with metadata about its class, namespace, template, and relationships to other components.
    /// </summary>
    /// <remarks>This class provides properties to describe a component's structure, including its class name,
    /// namespace, associated template, parent-child relationships, and implemented interfaces. It is designed to model
    /// hierarchical and compositional relationships between components.</remarks>
    public class Component
    {
        // Properties
        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// Gets or sets the namespace associated with the current Component.
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// Gets or sets the template used to define the structure and behavior of the component.
        /// </summary>
        public ComponentTemplate Template { get; set; }
        /// <summary>
        /// Gets the fully qualified name of the class, including its namespace.
        /// </summary>
        public string FullName { get { return $"{Namespace}.{ClassName}"; } }
        /// <summary>
        /// Gets or sets the parent component of the current component.
        /// </summary>
        public Component ParentComponent { get; set; }
        /// <summary>
        /// Gets or sets the list of interface types implemented by the current object.
        /// </summary>
        public List<Type> Interfaces { get; set; }
        /// <summary>
        /// Gets or sets the base type of the object represented by it's coresponding instance.
        /// </summary>
        public Type TypeOBaseType { get; set; } //TODO: Remove TypeOBaseType
        /// <summary>
        /// Gets or sets the collection of child components associated with this component.
        /// </summary>
        public List<Component> Children { get; set; }
        /// <summary>
        /// Gets or sets the list of properties associated with this component.
        /// </summary>
        public List<Property> Properties { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Component"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the <see cref="Interfaces"/>, <see cref="Children"/>, and <see cref="Properties"/>
        /// properties as empty lists, preparing the component for use.</remarks>
        public Component()
        {
            Interfaces = new List<Type>();
            Children = new List<Component>();
            Properties = new List<Property>();
        }
    }
}
