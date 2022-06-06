using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TypeD.Models.Data;
using TypeD.Models.Data.SettingContexts;
using TypeD.Models.Interfaces;
using TypeD.View;
using TypeD.View.Viewer;

namespace TypeD.Models
{
    internal class PanelModel : IPanelModel
    {
        // Models
        IUINotifyModel UINotifyModel { get; set; }
        ISettingModel SettingModel { get; set; }

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
            UINotifyModel = resourceModel.Get<IUINotifyModel>();
            SettingModel = resourceModel.Get<ISettingModel>();
        }

        // Functions
        public void OpenPanel(string id)
        {
            if (!Panels.ContainsKey(id))
                return;
            var panel = Panels[id];
            var setting = SettingModel.GetContext<MainWindowSettingContext>(SettingLevel.Local);
            var panelSetting = setting.Panels.Value.Find(p => p.ID == panel.ID);
            if (panelSetting == null)
            {
                panelSetting = new MainWindowSettingContext.Panel(panel.ID);
                setting.Panels.Value.Add(panelSetting);
            }

            if (panel.Open)
                return;

            panel.Open = true;
            panelSetting.Open = true;
            SettingModel.SetContext(setting);

            UINotifyModel.AddTo("MainWindow", panel);
        }

        public void ClosePanel(string id)
        {
            if (!Panels.ContainsKey(id))
                return;
            var panel = Panels[id];
            var setting = SettingModel.GetContext<MainWindowSettingContext>(SettingLevel.Local);
            var panelSetting = setting.Panels.Value.Find(p => p.ID == panel.ID);
            if (panelSetting == null)
            {
                panelSetting = new MainWindowSettingContext.Panel(panel.ID);
                setting.Panels.Value.Add(panelSetting);
            }

            if (!panel.Open)
                return;

            panel.Open = false;
            panelSetting.Open = false;
            SettingModel.SetContext(setting);

            UINotifyModel.RemoveFrom("MainWindow", panel);
        }

        public void AttachPanel(string id, string title, UIElement view)
        {
            if (Panels.ContainsKey(id))
                return;
            var panel = new Panel(id, title, view);
            Panels.Add(panel.ID, panel);

            var setting = SettingModel.GetContext<MainWindowSettingContext>(SettingLevel.Local);
            if(!setting.Panels.Value.Exists(p => p.ID == panel.ID))
            {
                setting.Panels.Value.Add(new MainWindowSettingContext.Panel(panel.ID));
                SettingModel.SetContext(setting);
            }
            else if(setting.Panels.Value.Find(p => p.ID == panel.ID).Open)
            {
                OpenPanel(id);
            }

            UINotifyModel.Notify("MainWindow", "Panels");
        }

        public void DetachPanel(string id)
        {
            if (!Panels.ContainsKey(id))
                return;

            var setting = SettingModel.GetContext<MainWindowSettingContext>(SettingLevel.Local);
            if (setting.Panels.Value.Exists(p => p.ID == id) && setting.Panels.Value.Find(p => p.ID == id).Open)
                ClosePanel(id);

            Panels.Remove(id);

            UINotifyModel.Notify("MainWindow", "Panels");
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

        public IViewer CreateViewer(string typeName)
        {
            if (!Viewers.Exists(v => v.FullName == typeName))
                return null;

            return Activator.CreateInstance(Viewers.Find(v => v.FullName == typeName)) as IViewer;
        }

        public List<string> ListViewers()
        {
            return Viewers.Select(v => v.FullName).ToList();
        }
    }
}
