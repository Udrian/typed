using System;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Defines the contract for managing components within a project, including adding, retrieving, opening, and
    /// closing components.
    /// </summary>
    /// <remarks>This interface provides methods for interacting with components in a project, such as adding 
    /// child components to a parent, retrieving the type of a component, and managing the lifecycle of components by
    /// opening or closing them. Implementations of this interface are expected to handle the underlying logic for
    /// these operations.</remarks>
    public interface IComponentModel : IModel
    {
        /// <summary>
        /// Adds a child component to a parent component within the specified project.
        /// </summary>
        /// <remarks>This method establishes a parent-child relationship between the specified components 
        /// within the given project. Ensure that both the parent and child components are valid.</remarks>
        /// <param name="project">The project to which the components belong. Cannot be <see langword="null"/>.</param>
        /// <param name="parent">The parent component to which the child will be added.</param>
        /// <param name="child">The child component to add to the parent. Cannot be <see langword="null"/>.</param>
        public void Add(Project project, Component parent, Component child);

        /// <summary>
        /// Retrieves the <see cref="Type"/> of the specified <see cref="Component"/>.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> whose type is to be retrieved. Cannot be <see langword="null"/>.</param>
        /// <returns>The <see cref="Type"/> of the specified <paramref name="component"/>.</returns>
        public Type GetType(Component component);

        /// <summary>
        /// Opens the specified project and component for editing or inspection.
        /// </summary>
        /// <remarks>This method prepares the specified project and component for use, such as loading
        /// necessary resources or initializing state. Ensure that both parameters are valid and non-null before
        /// calling this method.</remarks>
        /// <param name="project">The project to be opened. Cannot be <see langword="null"/>.</param>
        /// <param name="component">The component within the project to be opened. Cannot be <see langword="null"/>.</param>
        public void Open(Project project, Component component);

        /// <summary>
        /// Closes the specified project and component, releasing any associated resources.
        /// </summary>
        /// <remarks>This method ensures that the specified project and component are properly closed and
        /// any resources associated with them are released. After calling this method, the project and component
        /// should not be used unless reopened.</remarks>
        /// <param name="project">The project to be closed. Cannot be <see langword="null"/>.</param>
        /// <param name="component">The component within the project to be closed. Cannot be <see langword="null"/>.</param>
        public void Close(Project project, Component component);

        /// <summary>
        /// Determines whether the specified component is of the given type.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <param name="type">The type to compare against.</param>
        /// <returns><see langword="true"/> if the specified component is of the given type; otherwise, <see langword="false"/>.</returns>
        public bool IsOfType(Component component, Type type);

        /// <summary>
        /// Retrieves the base type of the specified <see cref="Component"/>.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> whose base type is to be determined. Cannot be <see langword="null"/>.</param>
        /// <returns>The <see cref="Type"/> representing the base type of the specified <see cref="Component"/>.
        /// Returns <see langword="null"/> if the component does not have a base type.</returns>
        public Type GetBaseType(Component component);
    }
}
