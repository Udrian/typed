using System;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;

namespace TypeD.Components
{
    /// <summary>
    /// Represents a template for creating and initializing <see cref="Component"/> within a system.
    /// </summary>
    /// <remarks>This abstract class provides a base for defining component templates, including methods for 
    /// initializing <see cref="Component"/>, filtering child <see cref="Component"/>, and generating component-specific codes. Derived classes
    /// must implement the abstract methods to provide specific behavior.</remarks>
    public abstract class ComponentTemplate
    {
        // Properties
        /// <summary>
        /// Gets the code that represents the type of the component.
        /// </summary>
        public ComponentTypeCode Code { get; internal set; }
        /// <summary>
        /// Gets the component associated with the current object.
        /// </summary>
        public Component Component { get; internal set; }

        // Constructors
        internal ComponentTemplate() { }

        /// <summary>
        /// Initializes the instance, preparing it for use.
        /// </summary>
        /// <remarks>This method must be called before using the instance. The specific initialization
        /// behavior depends on the derived implementation.</remarks>
        public abstract void Init();

        // Functions
        /// <summary>
        /// Filters the children of the current object based on the specified filter criteria.
        /// </summary>
        /// <remarks>This method is abstract and must be implemented by a derived class. The
        /// implementation should apply the provided filter to determine which children meet the specified
        /// criteria.</remarks>
        /// <param name="filter">An instance of <see cref="FilterHelper"/> that defines the filtering criteria to apply.</param>
        public abstract void ChildrenFilter(FilterHelper filter);
    }

    /// <summary>
    /// Represents a template for creating and managing components of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the component code associated with this template. Must derive from <see cref="ComponentTypeCode"/>.</typeparam>
    public abstract class ComponentTemplate<T> : ComponentTemplate where T : ComponentTypeCode
    {
        // Properties
        /// <summary>
        /// Gets or sets the code associated with the current instance, cast to the specified type.
        /// </summary>
        /// <remarks>The setter is internal and can only be accessed within the assembly. Ensure that the
        /// base code is compatible with the specified type <typeparamref name="T"/> to avoid unexpected
        /// behavior.</remarks>
        public new T Code { get { return base.Code as T; } internal set { base.Code = value; } }

        /// <summary>
        /// Initializes the component by generating the necessary code for its operation.
        /// </summary>
        /// <remarks>This method should be called to prepare the component for use. It ensures that the
        /// required code is created based on the current state of the <see cref="Component"/> property.</remarks>
        public override void Init()
        {
            Code = Activator.CreateInstance(typeof(T), Component) as T;
        }
    }
}
