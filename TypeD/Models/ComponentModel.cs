using System;
using System.Collections.Generic;
using System.Linq;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models
{
    internal class ComponentModel : IComponentModel
    {
        // Provider
        IComponentProvider ComponentProvider { get; set; }

        // Model
        IUINotifyModel UINotifyModel { get; set; }
        IProjectModel ProjectModel { get; set; }
        IHookModel HookModel { get; set; }

        // Data
        List<Component> OpenComponents { get; set; }

        // Constructors
        public ComponentModel()
        {
            OpenComponents = new List<Component>();
        }

        public void Init(IResourceModel resourceModel)
        {
            ComponentProvider = resourceModel.Get<IComponentProvider>();
            UINotifyModel = resourceModel.Get<IUINotifyModel>();
            ProjectModel = resourceModel.Get<IProjectModel>();
            HookModel = resourceModel.Get<IHookModel>();
        }

        // Functions
        public void Add(Project project, Component parent, Component child)
        {
            parent.Children.Add(child);
            ComponentProvider.Save(project, parent);
            ProjectModel.SaveCode(parent.Template.Code);

            // TODO: This should only happen through UINotifyModel and not with HookModel
            UINotifyModel.Notify("Nodes");
            HookModel.Shoot(new ComponentFocusHook() { Project = project, Component = parent });
        }

        public Type GetType(Component component)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName.Equals(component.FullName));
        }

        public void Open(Project project, Component component)
        {
            if(!OpenComponents.Exists(c => c.FullName == component.FullName))
            {
                HookModel.Shoot(new OpenComponentHook() { Project = project, Component = component });
                OpenComponents.Add(component);
            }
            HookModel.Shoot(new ComponentFocusHook() { Project = project, Component = component });
        }

        public void Close(Project project, Component component)
        {
            if (OpenComponents.Exists(c => c.FullName == component.FullName))
            {
                HookModel.Shoot(new CloseComponentHook() { Project = project, Component = component });
                OpenComponents.RemoveAll(c => c.FullName == component.FullName);
            }
        }
    }
}
