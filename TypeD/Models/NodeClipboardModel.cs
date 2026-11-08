using Avalonia.Input;
using Avalonia.Input.Platform;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Models
{
    internal sealed class NodeClipboardModel : INodeClipboardModel
    {
        private const string Prefix = "TypeO.NodeClipboard:1\n";
        private IComponentProvider ComponentProvider;
        private IComponentModel ComponentModel;
        private ICommandHistoryModel History;
        private IHookModel Hooks;
        private IFormatVersionModel Formats;
        private INodeSelectionModel Selection;
        private ComponentChildDTO Snapshot;

        internal Func<Task<string>> ReadText { get; set; } = async () =>
        {
            var clipboard = ViewModelBase.MainWindow.Clipboard
                ?? throw new InvalidOperationException("The system clipboard is unavailable.");
            using var data = await clipboard.TryGetDataAsync();
            return data == null ? null : await data.TryGetTextAsync();
        };
        internal Func<string, Task> WriteText { get; set; } = text =>
            (ViewModelBase.MainWindow.Clipboard
                ?? throw new InvalidOperationException("The system clipboard is unavailable.")).SetTextAsync(text);

        public void Init(IResourceModel resourceModel)
        {
            ComponentProvider = resourceModel.Get<IComponentProvider>();
            ComponentModel = resourceModel.Get<IComponentModel>();
            History = resourceModel.Get<ICommandHistoryModel>();
            Hooks = resourceModel.Get<IHookModel>();
            Formats = resourceModel.Get<IFormatVersionModel>();
            Selection = resourceModel.Get<INodeSelectionModel>();
        }

        public async Task RefreshAsync()
        {
            Snapshot = null;
            var text = await ReadText();
            if (text?.StartsWith(Prefix, StringComparison.Ordinal) != true)
                return;
            var document = JsonNode.Parse(text.Substring(Prefix.Length)) as JsonObject
                ?? throw new InvalidDataException("The node clipboard must contain a JSON object.");
            var storedVersions = document["FormatVersions"]?.Deserialize<System.Collections.Generic.Dictionary<string, int>>()
                ?? throw new InvalidDataException("The node clipboard has no format versions.");
            var currentVersions = Formats.GetCurrentVersions(FormatDocumentKind.Component);
            if (storedVersions.Count != currentVersions.Count ||
                storedVersions.Any(pair => !currentVersions.TryGetValue(pair.Key, out var version) || version != pair.Value))
                throw new InvalidDataException("The copied node uses different module formats. Copy it again with the current modules.");
            var snapshot = document["Node"]?.Deserialize<ComponentChildDTO>(JSON.CreateOptions())
                ?? throw new InvalidDataException("The node clipboard has no node.");
            Validate(snapshot);
            Snapshot = snapshot;
        }

        private static void Validate(ComponentChildDTO node)
        {
            if (string.IsNullOrWhiteSpace(node.FullName) || node.Properties == null || node.Children == null ||
                node.Properties.Any(property => property == null || string.IsNullOrWhiteSpace(property.Name)))
                throw new InvalidDataException("The node clipboard contains an invalid node.");
            foreach (var child in node.Children)
            {
                if (child == null)
                    throw new InvalidDataException("The node clipboard contains an invalid child.");
                Validate(child);
            }
        }

        public bool CanCopy(NodeClipboardCommandData data) =>
            IsCurrentTarget(data) && IsAttached(data.Node);

        public bool CanDuplicate(NodeClipboardCommandData data) =>
            CanCopy(data) && data.Node.ParentComponent != null && Allows(data.Node.ParentComponent, data.Node.RootBaseComponent.FullName) &&
            !Ancestors(data.Node.ParentComponent).Any(ancestor => ancestor.FullName == data.Node.FullName);

        public bool CanPaste(NodeClipboardCommandData data) =>
            IsCurrentTarget(data) && Snapshot != null && IsAttached(data.Node) &&
            AllowsSnapshot(data.Project, data.Node, Snapshot);

        private bool IsCurrentTarget(NodeClipboardCommandData data) =>
            data?.Project is { IsClosing: false } && data.Node != null &&
            Selection.Project == data.Project && Ancestors(data.Node).Last() == Selection.OpenedComponent;

        private bool AllowsSnapshot(Project project, Component parent, ComponentChildDTO snapshot)
        {
            var definition = ComponentProvider.Load(project, snapshot.FullName);
            return definition != null && Allows(parent, definition.RootBaseComponent.FullName) &&
                !Ancestors(parent).Any(ancestor => ancestor.FullName == snapshot.FullName) &&
                snapshot.Children.All(child => HasDefinitions(project, child));
        }

        private bool HasDefinitions(Project project, ComponentChildDTO snapshot) =>
            ComponentProvider.Load(project, snapshot.FullName) != null &&
            snapshot.Children.All(child => HasDefinitions(project, child));

        private static System.Collections.Generic.IEnumerable<Component> Ancestors(Component node)
        {
            for (; node != null; node = node.ParentComponent)
                yield return node;
        }

        private static bool IsAttached(Component node) => !Ancestors(node).Any(ancestor => ancestor.Removed);

        private static bool Allows(Component parent, string rootType)
        {
            if (parent.ReadOnly || !IsAttached(parent))
                return false;
            var filter = new FilterHelper();
            parent.Template?.ChildrenFilter(filter);
            return (filter.Filters ?? string.Empty).Contains(rootType + ";", StringComparison.Ordinal);
        }

        public async Task CopyAsync(NodeClipboardCommandData data)
        {
            if (!CanCopy(data))
                throw new InvalidOperationException("The selected node cannot be copied.");
            var snapshot = ComponentProvider.CaptureNode(data.Node);
            var document = new JsonObject
            {
                ["FormatVersions"] = JsonSerializer.SerializeToNode(Formats.GetCurrentVersions(FormatDocumentKind.Component)),
                ["Node"] = JsonSerializer.SerializeToNode(snapshot, JSON.CreateOptions())
            };
            await WriteText(Prefix + document.ToJsonString(JSON.CreateOptions()));
            Snapshot = snapshot;
        }

        public async Task PasteAsync(NodeClipboardCommandData data)
        {
            await RefreshAsync();
            if (!CanPaste(data))
                throw new InvalidOperationException("The clipboard does not contain a compatible node for the selected parent.");
            Insert(data, Snapshot, data.Node, data.Node.Children.Count, "Paste node");
        }

        public void Duplicate(NodeClipboardCommandData data)
        {
            if (!CanDuplicate(data))
                throw new InvalidOperationException("The selected node cannot be duplicated.");
            var parent = data.Node.ParentComponent;
            Insert(new NodeClipboardCommandData { Project = data.Project, Node = data.Node },
                ComponentProvider.CaptureNode(data.Node), parent, parent.Children.IndexOf(data.Node) + 1, "Duplicate node");
        }

        private void Insert(NodeClipboardCommandData data, ComponentChildDTO snapshot, Component parent, int index, string title)
        {
            var copy = ComponentProvider.RestoreNode(data.Project, snapshot);
            if (data.WorldPosition is { } position)
            {
                Hooks.Shoot(new NodeClipboardPositionHook
                {
                    Project = data.Project,
                    Parent = parent,
                    Node = copy,
                    X = position.X,
                    Y = position.Y
                });
            }
            ComponentModel.AddExisting(data.Project, parent, copy, index);
            Focus(data.Project, copy);
            History.Record(title, $"{title}: {copy.Name}.",
                () =>
                {
                    ComponentModel.Remove(data.Project, copy);
                    Focus(data.Project, parent);
                },
                () =>
                {
                    ComponentModel.AddExisting(data.Project, parent, copy, index);
                    Focus(data.Project, copy);
                });
        }

        private void Focus(Project project, Component node) =>
            Hooks.Shoot(new ComponentFocusHook { Project = project, Component = node, Root = false });
    }
}
