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
        /// Gets the unique identifier associated with the current object.
        /// </summary>
        public string ID
        {
            get
            {
                var idProperty = Properties.FirstOrDefault(p => p.Name == "ID");
                if (idProperty != null)
                    return idProperty.Value as string;
                return "";
            }
            set
            {
                var idProperty = Properties.FirstOrDefault(p => p.Name == "ID");
                if (idProperty != null)
                    idProperty.Value = value;
            }
        }
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
        /// Gets or sets the base inherited component of the current component.
        /// </summary>
        public Component BaseInheritedComponent { get;  set; }
        /// <summary>
        /// Gets the parent component of the current component, representing a hierarchical relationship between components.
        /// </summary>
        public Component ParentComponent { get; internal set; }
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
        /// Gets or sets the collection of properties that are explicitly overridden by this instance.
        /// </summary>
        public List<Property> OveriddenProperties { get; set; }

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
            OveriddenProperties = new List<Property>();
        }
    }
}
