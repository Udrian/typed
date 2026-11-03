using Dock.Model.Core;
using TypeD.Models.Data;
using TypeD.View;
using TypeD.View.Viewer;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that manages docking panels and viewers.</summary>
    public interface IPanelModel : IModel
    {
        /// <summary>Opens a panel.</summary>
        /// <param name="id">The panel id.</param>
        public void OpenPanel(string id);
        /// <summary>Closes a panel.</summary>
        /// <param name="id">The panel id.</param>
        public void ClosePanel(string id);
        /// <summary>Records the open state of a panel without firing a status change notification.</summary>
        /// <param name="id">The panel id.</param>
        /// <param name="open">Whether the panel is open.</param>
        public void SetPanelStatusWithoutNotify(string id, bool open);
        /// <summary>Registers a panel.</summary>
        /// <param name="id">The panel id.</param>
        /// <param name="title">The panel title.</param>
        /// <param name="view">The control shown in the panel.</param>
        /// <param name="defaultDockPosition">Where the panel is docked by default.</param>
        public void AttachPanel(string id, string title, Avalonia.Controls.Control view, Alignment defaultDockPosition = Alignment.Unset);
        /// <summary>Unregisters a panel.</summary>
        /// <param name="id">The panel id.</param>
        public void DetachPanel(string id);
        /// <summary>Gets all registered panels.</summary>
        /// <returns>The panels.</returns>
        public List<Panel> GetPanels();
        /// <summary>Registers a viewer type.</summary>
        /// <typeparam name="T">The viewer type.</typeparam>
        public void AddViewer<T>() where T : IViewer, new();
        /// <summary>Unregisters a viewer type.</summary>
        /// <typeparam name="T">The viewer type.</typeparam>
        public void RemoveViewer<T>() where T : IViewer, new();
        /// <summary>Creates a viewer instance by type name.</summary>
        /// <param name="project">The project the viewer is for.</param>
        /// <param name="typeName">The viewer type name.</param>
        /// <returns>The viewer.</returns>
        public IViewer CreateViewer(Project project, string typeName);
        /// <summary>Lists the registered viewer type names.</summary>
        /// <returns>The viewer names.</returns>
        public List<string> ListViewers();
    }
}
