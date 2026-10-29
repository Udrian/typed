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

        public string FullParentPath
        {
            get
            {
                if (ParentComponent == null)
                    return "";
                return ParentComponent.FullParentPath + ParentComponent.ID + "/";
            }
        }

        public string Name
        {
            get
            {
                var nameProperty = Properties.FirstOrDefault(p => p.Name == "Name");
                if (nameProperty != null && !string.IsNullOrEmpty(nameProperty.Value as string))
                    return nameProperty.Value as string;
                return ClassName;
            }
            set
            {
                var nameProperty = Properties.FirstOrDefault(p => p.Name == "Name");
                if (nameProperty != null)
                    nameProperty.Value = value;
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
        /// Gets or sets the children defined by this component's own definition.
        /// </summary>
        public List<Component> DefinitionChildren { get; set; }
        /// <summary>
        /// Gets or sets the list of properties associated with this component.
        /// </summary>
        public List<Property> Properties { get; set; }
        /// <summary>
        /// Gets or sets the collection of properties that are explicitly overridden by this instance.
        /// </summary>
        public List<Property> OveriddenProperties { get; set; }

        /// <summary>
        /// Gets or sets whether the component definition can't be modified when opened on its own. Base type components
        /// are read only, but instances of them added as children to other components are not.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// The file name of the icon used when no icon has been selected anywhere in the inheritance chain.
        /// </summary>
        public const string DefaultIcon = "default.png";
        /// <summary>
        /// Gets or sets the file name of the icon selected for this component type, or <see langword="null"/> to inherit it from the base component.
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// Gets the file name of the icon in use, which is the selected icon or the one inherited from the base components.
        /// </summary>
        public string ResolvedIcon
        {
            get
            {
                return !string.IsNullOrEmpty(Icon) ? Icon : BaseInheritedComponent?.ResolvedIcon ?? DefaultIcon;
            }
        }
        /// <summary>
        /// Gets or sets whether this child, inherited from the definition of its parent, has been removed from the parent instance.
        /// </summary>
        public bool Removed { get; set; }
        /// <summary>
        /// Gets or sets whether this child is exposed to components that contain the component owning it. Children are private by default.
        /// </summary>
        public bool Public { get; set; }

        /// <summary>
        /// Gets whether this node can be seen and modified from the opened component. Nodes that come from the definition
        /// of a nested component are only reachable when they, and every inherited node above them, are public.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                for (var node = this; node != null; node = node.ParentComponent)
                {
                    if (node.IsInherited && !node.Public)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets whether the public state of this node can be toggled from the opened component.
        /// </summary>
        public bool CanTogglePublic
        {
            get
            {
                return ParentComponent != null && !IsInherited && !ParentComponent.ReadOnly && !Removed;
            }
        }

        /// <summary>
        /// Gets whether this node comes from the definition of the component it is placed in, rather than being owned by the opened component.
        /// </summary>
        public bool IsInherited
        {
            get
            {
                return ParentComponent?.ParentComponent != null && ParentComponent.DefinitionChildren.Contains(this);
            }
        }

        /// <summary>
        /// Gets whether the order of the children differs from the default order, which is the definition children
        /// in definition order followed by the added children.
        /// </summary>
        public bool ChildOrderChanged
        {
            get
            {
                var defaultOrder = DefinitionChildren.Where(Children.Contains)
                    .Concat(Children.Where(child => !DefinitionChildren.Contains(child)));
                return !Children.SequenceEqual(defaultOrder);
            }
        }
        /// <summary>
        /// Gets whether any property differs from its value in the current component context.
        /// </summary>
        public bool HasPropertyOverrides
        {
            get
            {
                return Properties.Any(property => HasPropertyOverridesFor(property.Name));
            }
        }

        /// <summary>
        /// Gets whether the named property differs from its value in the current component context.
        /// </summary>
        public bool HasPropertyOverridesFor(string propertyName)
        {
            var property = Properties.FirstOrDefault(p => p.Name == propertyName);
            if (property == null)
                return false;

            var original = OveriddenProperties.FirstOrDefault(p => p.Name == propertyName);
            return original == null || !object.Equals(original.Value, property.Value);
        }

        /// <summary>
        /// Gets whether this component or any of its children has changes to persist.
        /// </summary>
        public bool HasChanges
        {
            get
            {
                return Removed || HasPropertyOverrides || ChildOrderChanged || Children.Any(child =>
                    !DefinitionChildren.Contains(child) || child.HasChanges);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Component"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the <see cref="Interfaces"/>, <see cref="Children"/>, and <see cref="Properties"/>
        /// properties as empty lists, preparing the component for use.</remarks>
        public Component()
        {
            Interfaces = new List<Type>();
            Children = new List<Component>();
            DefinitionChildren = new List<Component>();
            Properties = new List<Property>();
            OveriddenProperties = new List<Property>();
        }
    }
}
