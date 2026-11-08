using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal sealed class NodeSelectionModel : INodeSelectionModel, IDisposable
    {
        private enum ActiveTree
        {
            None,
            ComponentExplorer,
            ComponentBrowser
        }

        private IHookModel HookModel { get; set; }
        private readonly Action<ComponentFocusHook> ComponentFocusedHandler;
        private readonly Action<CloseComponentHook> ComponentClosedHandler;
        private readonly Action<ComponentRemovedHook> ComponentRemovedHandler;

        public Project Project { get; private set; }
        public Component OpenedComponent { get; private set; }
        public Component SelectedNode { get; private set; }
        public IReadOnlyList<Component> SelectedNodes { get; private set; } = Array.Empty<Component>();
        public Component SelectedComponentType { get; private set; }
        public bool IsComponentExplorerActive => _activeTree == ActiveTree.ComponentExplorer;
        public bool IsComponentBrowserActive => _activeTree == ActiveTree.ComponentBrowser;

        private ActiveTree _activeTree;

        public NodeSelectionModel()
        {
            ComponentFocusedHandler = OnComponentFocused;
            ComponentClosedHandler = OnComponentClosed;
            ComponentRemovedHandler = OnComponentRemoved;
        }

        public void Init(IResourceModel resourceModel)
        {
            HookModel = resourceModel.Get<IHookModel>();
            HookModel.AddHook(ComponentFocusedHandler);
            HookModel.AddHook(ComponentClosedHandler);
            HookModel.AddHook(ComponentRemovedHandler);
        }

        public void Dispose()
        {
            HookModel?.RemoveHook(ComponentFocusedHandler);
            HookModel?.RemoveHook(ComponentClosedHandler);
            HookModel?.RemoveHook(ComponentRemovedHandler);
        }

        private void OnComponentFocused(ComponentFocusHook hook)
        {
            if (hook.Root)
            {
                Project = hook.Project;
                OpenedComponent = hook.Component;
                SelectedNode = hook.Component;
                SelectedNodes = new[] { hook.Component };
                return;
            }

            if (OpenedComponent == null || !ReferenceEquals(Project, hook.Project))
                return;

            if (ReferenceEquals(GetRootComponent(hook.Component), OpenedComponent))
            {
                SelectedNode = hook.Component;
                SelectedNodes = hook.Selection != null && hook.Selection.Contains(hook.Component)
                    ? hook.Selection.ToList()
                    : new[] { hook.Component };
                _activeTree = ActiveTree.ComponentExplorer;
            }
        }

        private void OnComponentClosed(CloseComponentHook hook)
        {
            if (!ReferenceEquals(Project, hook.Project) || !ReferenceEquals(OpenedComponent, hook.Component))
                return;

            Project = null;
            OpenedComponent = null;
            SelectedNode = null;
            SelectedNodes = Array.Empty<Component>();
            _activeTree = ActiveTree.None;
        }

        private void OnComponentRemoved(ComponentRemovedHook hook)
        {
            if (SelectedNode != null && IsWithin(SelectedNode, hook.Child))
                SelectedNode = OpenedComponent;

            if (SelectedNodes.Any(node => IsWithin(node, hook.Child)))
            {
                var remaining = SelectedNodes.Where(node => !IsWithin(node, hook.Child)).ToList();
                if (remaining.Count == 0 && SelectedNode != null)
                    remaining.Add(SelectedNode);
                SelectedNodes = remaining;
            }
        }

        public void SetComponentExplorerActive()
        {
            _activeTree = ActiveTree.ComponentExplorer;
        }

        public void SetNoTreeActive()
        {
            _activeTree = ActiveTree.None;
        }

        public void SetComponentBrowserActive(Project project, Component component)
        {
            Project = project;
            SelectedComponentType = component;
            _activeTree = ActiveTree.ComponentBrowser;
        }

        private static Component GetRootComponent(Component component)
        {
            while (component?.ParentComponent != null)
                component = component.ParentComponent;
            return component;
        }

        private static bool IsWithin(Component component, Component ancestor)
        {
            while (component != null)
            {
                if (ReferenceEquals(component, ancestor))
                    return true;
                component = component.ParentComponent;
            }
            return false;
        }
    }
}
