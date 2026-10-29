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
            var childInstance = ComponentProvider.CreateInstance(project, child);
            AddExisting(project, parent, childInstance);
        }

        public void AddExisting(Project project, Component parent, Component child)
        {
            AddExisting(project, parent, child, -1);
        }

        public void AddExisting(Project project, Component parent, Component child, int index)
        {
            if (child.ParentComponent != null)
                throw new InvalidOperationException("The component already belongs to a parent.");

            if (index < 0 || index > parent.Children.Count)
                index = parent.Children.Count;
            parent.Children.Insert(index, child);
            child.ParentComponent = parent;
            if (string.IsNullOrEmpty(child.ID))
                child.ID = Guid.NewGuid().ToString();
            var rootComponent = GetRootComponent(parent);
            ComponentProvider.Save(project, rootComponent);
            ProjectModel.SaveCode(rootComponent.Template.Code);

            HookModel.Shoot(new ComponentAddedHook() { Parent = parent, Child = child, Index = index });
        }

        public void Reorder(Project project, Component child, int index)
        {
            var parent = child.ParentComponent;
            if (parent == null)
                return;

            parent.Children.Remove(child);
            if (index < 0 || index > parent.Children.Count)
                index = parent.Children.Count;
            parent.Children.Insert(index, child);

            var rootComponent = GetRootComponent(parent);
            ComponentProvider.Save(project, rootComponent);
            ProjectModel.SaveCode(rootComponent.Template.Code);

            HookModel.Shoot(new ComponentMovedHook() { Parent = parent, Child = child, Index = index });
        }

        public void Remove(Project project, Component child)
        {
            var parent = child.ParentComponent;
            if (parent == null)
            {
                return;
            }

            parent.Children.Remove(child);
            var rootComponent = GetRootComponent(parent);
            ComponentProvider.Save(project, rootComponent);
            ProjectModel.SaveCode(rootComponent.Template.Code);

            HookModel.Shoot(new ComponentRemovedHook() { Parent = parent, Child = child });
            child.ParentComponent = null;
        }

        public void SetRemoved(Project project, Component child, bool removed)
        {
            var parent = child.ParentComponent;
            if (parent == null || child.Removed == removed)
                return;

            child.Removed = removed;
            var rootComponent = GetRootComponent(parent);
            ComponentProvider.Save(project, rootComponent);
            ProjectModel.SaveCode(rootComponent.Template.Code);

            HookModel.Shoot(new ComponentRemovedStateChangedHook() { Parent = parent, Child = child });
        }

        public void SetPublic(Project project, Component child, bool isPublic)
        {
            var parent = child.ParentComponent;
            if (parent == null || child.Public == isPublic)
                return;

            child.Public = isPublic;
            var rootComponent = GetRootComponent(parent);
            ComponentProvider.Save(project, rootComponent);
            ProjectModel.SaveCode(rootComponent.Template.Code);

            HookModel.Shoot(new ComponentPublicStateChangedHook() { Parent = parent, Child = child });
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

        public bool IsOfType(Component component, Type type)
        {
            if (component == null)
            {
                return false;
            }
            if (component.FullName == type.FullName)
            {
                return true;
            }
            return IsOfType(component.BaseInheritedComponent, type);
        }

        public Type GetBaseType(Component component)
        {
            if(component.BaseInheritedComponent == null)
            {
                return GetType(component);
            }
            return GetBaseType(component.BaseInheritedComponent);
        }

        private static Component GetRootComponent(Component component)
        {
            while (component.ParentComponent != null)
            {
                component = component.ParentComponent;
            }

            return component;
        }
    }
}
