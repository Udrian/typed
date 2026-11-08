using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Tracks the active project, tree selection, and component browser context.
    /// </summary>
    public interface INodeSelectionModel : IModel
    {
        /// <summary>
        /// Gets the currently active project.
        /// </summary>
        Project Project { get; }
        /// <summary>
        /// Gets the component currently opened in the viewer.
        /// </summary>
        Component OpenedComponent { get; }
        /// <summary>
        /// Gets the primary selected node.
        /// </summary>
        Component SelectedNode { get; }
        /// <summary>
        /// All selected nodes in tree order. Contains <see cref="SelectedNode"/> when it isn't null.
        /// </summary>
        IReadOnlyList<Component> SelectedNodes { get; }
        /// <summary>
        /// Gets the selected component type.
        /// </summary>
        Component SelectedComponentType { get; }
        /// <summary>
        /// Gets whether the component explorer is active.
        /// </summary>
        bool IsComponentExplorerActive { get; }
        /// <summary>
        /// Gets whether the component browser is active.
        /// </summary>
        bool IsComponentBrowserActive { get; }

        /// <summary>
        /// Makes the component explorer the active tree.
        /// </summary>
        void SetComponentExplorerActive();
        /// <summary>
        /// Clears the active tree selection context.
        /// </summary>
        void SetNoTreeActive();
        /// <summary>
        /// Makes the component browser active for a project and component.
        /// </summary>
        void SetComponentBrowserActive(Project project, Component component);
    }
}
