using System.Collections.Generic;
using System.Windows;
using TypeD.View;
using TypeD.View.Viewer;

namespace TypeD.Models.Interfaces
{
    public interface IPanelModel : IModel
    {
        public void OpenPanel(string id);
        public void ClosePanel(string id);
        public void AttachPanel(string id, string title, UIElement view);
        public void DetachPanel(string id);
        public List<Panel> GetPanels();
        public void AddViewer<T>() where T : IViewer, new();
        public void RemoveViewer<T>() where T : IViewer, new();
        public IViewer CreateViewer(string typeName);
        public List<string> ListViewers();
    }
}
