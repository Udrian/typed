using TypeD.Components;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    /// <summary>
    /// Defines methods for creating, managing, and interacting with components within a project.
    /// </summary>
    /// <remarks>This interface provides functionality for creating, saving, loading, deleting, renaming, and
    /// querying components. It also includes methods for retrieving component paths and listing all components in a
    /// project.</remarks>
    public interface IComponentProvider : IProvider
    {
        /// <summary>
        /// Creates a new component template based on the specified project, class name, namespace, and component
        /// details.
        /// </summary>
        /// <param name="project">The project in which the component template will be created.</param>
        /// <param name="className">The name of the class to be generated for the component template.</param>
        /// <param name="namespace">The namespace to assign to the generated component class.</param>
        /// <param name="parentComponent">The component details used to define the structure and behavior of the template.</param>
        /// <param name="interfaces">An optional list of interface names that the generated component class will implement. Can be <see
        /// langword="null"/> if no interfaces are required.</param>
        /// <returns>A <see cref="ComponentTemplate"/> representing the newly created component template.</returns>
        public ComponentTemplate Create(Project project, string className, string @namespace, Component parentComponent, List<string> interfaces = null);

        /// <summary>
        /// Saves the specified component.
        /// </summary>
        /// <remarks>This method persists the provided project and component to the underlying storage. 
        /// Ensure that both parameters are valid and initialized before calling this method.</remarks>
        /// <param name="project">The current project context. Cannot be <see langword="null"/>.</param>
        /// <param name="component">The component associated with the project to be saved. Cannot be <see langword="null"/>.</param>
        public void Save(Project project, Component component);

        /// <summary>
        /// Loads a component from the specified project using its fully qualified name.
        /// </summary>
        /// <param name="project">The project from which to load the component. Cannot be <see langword="null"/>.</param>
        /// <param name="fullName">The fully qualified name of the component to load. Cannot be <see langword="null"/> or empty.</param>
        /// <returns>The loaded <see cref="Component"/> instance if found; otherwise, <see langword="null"/>.</returns>
        public Component Load(Project project, string fullName);

        /// <summary>
        /// Deletes the specified component from the given project.
        /// </summary>
        /// <param name="project">The project from which the component will be deleted. Cannot be <see langword="null"/>.</param>
        /// <param name="component">The component to delete from the project. Cannot be <see langword="null"/>.</param>
        public void Delete(Project project, Component component);

        /// <summary>
        /// Renames the specified component within the given project to the provided class name.
        /// </summary>
        /// <param name="project">The project containing the component to be renamed.</param>
        /// <param name="component">The component to rename. Must belong to the specified project.</param>
        /// <param name="newClassName">The new class name to assign to the component. Cannot be <see langword="null"/> or empty.</param>
        public void Rename(Project project, Component component, string newClassName);

        /// <summary>
        /// Determines whether the specified component exists within the given project.
        /// </summary>
        /// <param name="project">The project to search within. Cannot be <see langword="null"/>.</param>
        /// <param name="component">The component to check for existence. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the specified component exists within the project; otherwise, <see langword="false"/>.</returns>
        public bool Exists(Project project, Component component);

        /// <summary>
        /// Determines whether a project contains a component of the specified type.
        /// </summary>
        /// <param name="project">The project to search for the component. Cannot be <see langword="null"/>.</param>
        /// <param name="type">The type of the component to locate. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the project contains a component of the specified type; otherwise, <see langword="false"/>.</returns>
        public bool Exists(Project project, Type type);

        /// <summary>
        /// Retrieves a list of all components associated with the specified project.
        /// </summary>
        /// <param name="project">The project for which to retrieve the components. Cannot be <see langword="null"/>.</param>
        /// <returns>A list of <see cref="Component"/> objects associated with the specified project. Returns an empty list if no
        /// components are found.</returns>
        public List<Component> ListAll(Project project);

        /// <summary>
        /// Retrieves the file system path associated with the specified project and component.
        /// </summary>
        /// <param name="project">The project for which the path is being retrieved. Cannot be <see langword="null"/>.</param>
        /// <param name="component">The component within the project whose path is being retrieved. Cannot be <see langword="null"/>.</param>
        /// <returns>The file system path as a <see cref="string"/> that corresponds to the specified project and component.</returns>
        public string GetPath(Project project, Component component);

        /// <summary>
        /// Retrieves the file path of a specified item within a project.
        /// </summary>
        /// <param name="project">The project containing the item.</param>
        /// <param name="fullName">The full name of the item whose path is to be retrieved.</param>
        /// <returns>The file path of the specified item if found; otherwise, an empty string.</returns>
        public string GetPath(Project project, string fullName);

        /// <summary>
        /// Adds a base type component to the current collection.
        /// </summary>
        /// <remarks>This method adds the specified component to the collection. Ensure that the component
        /// is not already part of the collection to avoid potential duplication issues.</remarks>
        /// <param name="component">The component to add. This parameter cannot be <see langword="null"/>.</param>
        public void AddBaseTypeComponent(Component component);

        /// <summary>
        /// Removes the specified component from the base type.
        /// </summary>
        /// <remarks>This method removes the provided component from the base type's collection of
        /// components. Ensure that the component is part of the base type before calling this method.</remarks>
        /// <param name="component">The component to remove. Must not be <see langword="null"/>.</param>
        public void RemoveBaseTypeComponent(Component component);

        /// <summary>
        /// Retrieves a list of components that are associated with the base type.
        /// </summary>
        /// <returns>A list of <see cref="Component"/> objects representing the components of the base type. The list will be
        /// empty if no components are associated with the base type.</returns>
        public List<Component> GetBaseTypeComponents();
    }
}
