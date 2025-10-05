using System;
using TypeD.Models.Data;

namespace TypeD.Code
{
    /// <summary>
    /// Represents an abstract base class for defining and analyzing component type codes.
    /// </summary>
    /// <remarks>This class provides a framework for working with component type codes, including properties 
    /// to access the base type, determine if the component is a base type, and retrieve associated components. It is
    /// designed to be extended by derived classes that implement specific behavior for component type
    /// analysis.</remarks>
    public abstract class ComponentTypeCode : TypeDCodalyzer
    {
        // Properties
        /// <summary>
        /// Gets the base type of the current type.
        /// </summary>
        public abstract Type TypeOBaseType { get; }
        /// <summary>
        /// Gets a value indicating whether the current type is the base component type.
        /// </summary>
        public virtual bool IsBaseComponentType
        {
            get
            {
                return BaseClass == TypeOBaseType.FullName;
            }
        }

        /// <summary>
        /// Gets the component associated with the current instance.
        /// </summary>
        public Component Component { get; private set; }

        /// <summary>
        /// Gets the parent component of the current component.
        /// </summary>
        public Component ParentComponent { get { return Component.ParentComponent; } }

        // Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentTypeCode"/> class using the specified component.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> instance used to initialize this object. Must not be <see langword="null"/>.</param>
        public ComponentTypeCode(Component component)
        {
            ClassName = component.ClassName;
            Namespace = component.Namespace;
            Component = component;
            BaseClass = ParentComponent == null ? TypeOBaseType.FullName : ParentComponent.FullName;
        }
    }
}
