using Dock.Model.Core;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.View;
using TypeD.View.Viewer;

namespace TypeD.Models
{
    internal class PanelModel : IPanelModel
    {
        // Models
        IHookModel HookModel { get; set; }

        // Data
        Dictionary<string, Panel> Panels { get; set; }
        List<Type> Viewers { get; set; }

        // Constructors
        public PanelModel()
        {
            Panels = new Dictionary<string, Panel>();
            Viewers = new List<Type>();
        }

        public void Init(IResourceModel resourceModel)
        {
            HookModel = resourceModel.Get<IHookModel>();
        }

        // Functions
        public void OpenPanel(string id)
        {
            if (!Panels.ContainsKey(id))
                return;
            var panel = Panels[id];

            if (panel.Open)
                return;

            panel.Open = true;
            HookModel.Shoot(new PanelStatusChangedHook() { Panel = panel, Status = PanelStatusChangedHook.PanelStatus.Opened });
        }

        public void ClosePanel(string id)
        {
            if (!Panels.ContainsKey(id))
                return;
            var panel = Panels[id];

            if (!panel.Open)
                return;

            panel.Open = false;
            HookModel.Shoot(new PanelStatusChangedHook() { Panel = panel, Status = PanelStatusChangedHook.PanelStatus.Closed });
        }

        public void SetPanelStatusWithoutNotify(string id, bool open)
        {
            if (!Panels.ContainsKey(id))
                return;
            var panel = Panels[id];
            panel.Open = open;
        }

        public void AttachPanel(string id, string title, Avalonia.Controls.Control view, Alignment defaultDockPosition = Alignment.Unset)
        {
            if (Panels.ContainsKey(id))
                return;

            var panel = new Panel(id, title, view, defaultDockPosition);
            Panels.Add(panel.ID, panel);
            HookModel.Shoot(new PanelStatusChangedHook() { Panel = panel, Status = PanelStatusChangedHook.PanelStatus.Attached });
        }

        public void DetachPanel(string id)
        {
            if (!Panels.ContainsKey(id))
                return;

            var panel = Panels[id];
            Panels.Remove(id);
            HookModel.Shoot(new PanelStatusChangedHook() { Panel = panel, Status = PanelStatusChangedHook.PanelStatus.Detached });
        }

        public List<Panel> GetPanels()
        {
            return new List<Panel>(Panels.Values);
        }

        public void AddViewer<T>() where T : IViewer, new()
        {
            if (Viewers.Contains(typeof(T)))
                return;
            Viewers.Add(typeof(T));
        }

        public void RemoveViewer<T>() where T : IViewer, new()
        {
            if (!Viewers.Contains(typeof(T)))
                return;
            Viewers.Remove(typeof(T));
        }

        public IViewer CreateViewer(Project project, string typeName)
        {
            if (!Viewers.Exists(v => v.FullName == typeName))
                return null;

            var viewer = Activator.CreateInstance(Viewers.Find(v => v.FullName == typeName)) as IViewer;
            viewer.Project = project;
            return viewer;
        }

        public List<string> ListViewers()
        {
            return Viewers.Select(v => v.FullName).ToList();
        }
    }
}
