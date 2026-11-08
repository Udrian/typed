using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Avalonia.Controls.Templates;
using TypeD;
using TypeD.Commands;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.PropertyDefinitions;
using TypeD.Models.Data.Hooks;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using TypeD.View.Viewer;
using TypeD.ViewModel;

namespace TypeDTest;

public class LibraryModelApiTests
{
    [Fact]
    public void Init_RegistersPrimitiveEditorsWithoutEngineModules()
    {
        var properties = InitializeResources().Get<IPropertyModel>();
        foreach (var type in new[] { typeof(bool), typeof(string), typeof(int), typeof(double), typeof(Enum) })
        {
            var definition = properties.Get(type);
            Assert.NotNull(definition);
            Assert.Same(typeof(TypeDInit).Assembly, definition.GetType().Assembly);
        }
        Assert.IsType<DoublePropertyDefinition>(properties.Get(typeof(double)));
    }

    [Fact]
    public void ComponentMoveRules_RejectsMovesIntoOwnDescendants()
    {
        var parent = new Component();
        var node = new Component();
        var descendant = new Component();
        typeof(Component).GetProperty(nameof(Component.ParentComponent)).SetValue(node, parent);
        typeof(Component).GetProperty(nameof(Component.ParentComponent)).SetValue(descendant, node);
        Assert.False(TypeD.Models.ComponentMoveRules.TryResolve(node, descendant, MovePosition.Child, out _, out _));
    }

    [Fact]
    public void Init_RegistersBuiltInModelAndProviderInterfaces()
    {
        var resources = InitializeResources();

        Assert.NotNull(resources.Get<IComponentModel>());
        Assert.NotNull(resources.Get<IPropertyModel>());
        Assert.NotNull(resources.Get<IEditorCommandRegistry>());
        Assert.NotNull(resources.Get<ICommandHistoryModel>());
        Assert.Equal("TypeD.Models.CommandHistoryModel", resources.Get<ICommandHistoryModel>().GetType().FullName);
        Assert.Same(typeof(TypeDInit).Assembly, resources.Get<ICommandHistoryModel>().GetType().Assembly);
        foreach (var model in new IModel[] { resources.Get<INodeSelectionModel>(), resources.Get<INodeClipboardModel>(), resources.Get<IViewerCameraModel>() })
        {
            Assert.NotNull(model);
            Assert.Same(typeof(TypeDInit).Assembly, model.GetType().Assembly);
        }
        Assert.Same(typeof(TypeDInit).Assembly, typeof(INodeSelectionModel).Assembly);
        Assert.Same(typeof(TypeDInit).Assembly, typeof(INodeClipboardModel).Assembly);
        Assert.Same(typeof(TypeDInit).Assembly, typeof(IViewerCameraModel).Assembly);
        Assert.DoesNotContain(typeof(TypeDInit).Assembly.GetReferencedAssemblies(), assembly => assembly.Name is "TypeDCore" or "TypeOCore");
        Assert.NotNull(resources.Get<IContentProvider>());
        Assert.Same(resources, ViewModelBase.ResourceModel);
    }

    [Fact]
    public void CommandHistoryModel_UndoesAndRedoesWithReplayState()
    {
        var history = InitializeResources().Get<ICommandHistoryModel>();
        var value = 1;
        var changes = 0;
        history.HistoryChanged += (_, _) => changes++;
        history.Record("Change value", "Set value to one", () =>
        {
            Assert.True(history.IsReplaying);
            value = 0;
        }, () =>
        {
            Assert.True(history.IsReplaying);
            value = 1;
        });

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.True(history.Undo());
        Assert.Equal(0, value);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        Assert.False(history.IsReplaying);
        Assert.True(history.Redo());
        Assert.Equal(1, value);
        Assert.False(history.IsReplaying);
        Assert.Equal(3, changes);
    }

    [Fact]
    public void CommandHistoryModel_ProjectLoadStartsWithFreshHistory()
    {
        var resources = InitializeResources();
        var projectModel = resources.Get<IProjectModel>();
        projectModel.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<ISettingModel>().Init(resources);
        var directory = CreateTempDirectory();
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dto = JsonSerializer.Deserialize($$"""{"ProjectName":"HistoryTests-{{Guid.NewGuid():N}}"}""", constructor.GetParameters()[1].ParameterType);
        var project = (Project)constructor.Invoke(new[] { directory, dto });
        var previous = resources.Get<ICommandHistoryModel>();
        var previousSelection = resources.Get<INodeSelectionModel>();
        var previousCamera = resources.Get<IViewerCameraModel>();
        var previousClipboard = resources.Get<INodeClipboardModel>();
        previousSelection.Init(resources);
        previousSelection.SetComponentBrowserActive(project, new Component());
        previousCamera.Attach(new ViewerCamera());
        previous.Record("Previous command", "Previous project", () => { }, () => { });

        try
        {
            TypeDInit.ProjectLoad(project, resources);
            var history = resources.Get<ICommandHistoryModel>();

            Assert.NotSame(previous, history);
            Assert.Empty(history.Entries);
            Assert.False(history.CanUndo);
            Assert.False(history.CanRedo);
            Assert.NotSame(previousSelection, resources.Get<INodeSelectionModel>());
            Assert.Null(resources.Get<INodeSelectionModel>().Project);
            Assert.Empty(resources.Get<INodeSelectionModel>().SelectedNodes);
            Assert.NotSame(previousCamera, resources.Get<IViewerCameraModel>());
            Assert.Null(previousCamera.Camera);
            Assert.Null(resources.Get<IViewerCameraModel>().Camera);
            Assert.NotSame(previousClipboard, resources.Get<INodeClipboardModel>());
        }
        finally
        {
            projectModel.StopWatching(project);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void NodeSelectionModel_TracksSelectionRemovalAndDisposal()
    {
        var resources = InitializeResources();
        var selection = resources.Get<INodeSelectionModel>();
        selection.Init(resources);
        var hooks = resources.Get<IHookModel>();
        var root = new Component();
        var first = new Component();
        var second = new Component();
        typeof(Component).GetProperty(nameof(Component.ParentComponent)).SetValue(first, root);
        typeof(Component).GetProperty(nameof(Component.ParentComponent)).SetValue(second, root);
        root.Children.AddRange([first, second]);
        hooks.Shoot(new ComponentFocusHook { Component = root });
        hooks.Shoot(new ComponentFocusHook { Component = second, Root = false, Selection = new[] { first, second } });

        Assert.Same(root, selection.OpenedComponent);
        Assert.Same(second, selection.SelectedNode);
        Assert.Equal(new[] { first, second }, selection.SelectedNodes);
        Assert.True(selection.IsComponentExplorerActive);
        hooks.Shoot(new ComponentRemovedHook { Parent = root, Child = second });
        Assert.Same(root, selection.SelectedNode);
        Assert.Equal(new[] { first }, selection.SelectedNodes);

        ((IDisposable)selection).Dispose();
        hooks.Shoot(new ComponentFocusHook { Component = new Component() });
        Assert.Same(root, selection.OpenedComponent);
    }

    [Fact]
    public void ViewerCameraModel_AttachesClampsSpeedAndUnsubscribes()
    {
        var resources = InitializeResources();
        var model = resources.Get<IViewerCameraModel>();
        model.Init(resources);
        var camera = new ViewerCamera();
        var changes = 0;
        model.Changed += () => changes++;
        model.Attach(camera);
        Assert.Equal(ViewerCamera.DefaultSpeed, camera.Speed);

        model.Speed = ViewerCamera.MaxSpeed * 2;
        Assert.Equal(ViewerCamera.MaxSpeed, model.Speed);
        Assert.Equal(model.Speed, camera.Speed);
        model.Speed = double.NaN;
        Assert.Equal(ViewerCamera.MaxSpeed, model.Speed);
        camera.Move(1, 1);
        Assert.Equal(3, changes);
        model.Detach(camera);
        camera.Move(1, 1);
        Assert.Null(model.Camera);
        Assert.Equal(4, changes);
    }

    [Fact]
    public async Task NodeClipboardModel_CopiesIndependentSnapshotsAndClearsExternalClipboard()
    {
        var resources = InitializeResources();
        resources.Get<IComponentProvider>().Init(resources);
        resources.Get<IFormatVersionModel>().Init(resources);
        var clipboard = resources.Get<INodeClipboardModel>();
        clipboard.Init(resources);
        var selection = resources.Get<INodeSelectionModel>();
        selection.Init(resources);
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dto = JsonSerializer.Deserialize("""{"ProjectName":"ClipboardTests"}""", constructor.GetParameters()[1].ParameterType);
        var project = (Project)constructor.Invoke(new[] { Path.GetTempPath(), dto });
        var root = new Component { ClassName = "Root", Namespace = "Example" };
        var source = new Component { ClassName = "Child", Namespace = "Example" };
        typeof(Component).GetProperty(nameof(Component.ParentComponent)).SetValue(source, root);
        source.Properties.Add(new Property { Name = "Text", Type = typeof(string), Value = "original" });
        root.Children.Add(source);
        resources.Get<IHookModel>().Shoot(new ComponentFocusHook { Project = project, Component = root });
        var data = new NodeClipboardCommandData { Project = project, Node = source };
        string text = null;
        clipboard.GetType().GetProperty("ReadText", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(clipboard, (Func<Task<string>>)(() => Task.FromResult(text)));
        clipboard.GetType().GetProperty("WriteText", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(clipboard, (Func<string, Task>)(value => { text = value; return Task.CompletedTask; }));

        Assert.True(clipboard.CanCopy(data));
        await clipboard.CopyAsync(data);
        source.Properties[0].Value = "changed";
        await clipboard.RefreshAsync();
        var snapshotField = clipboard.GetType().GetField("Snapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        var snapshot = Assert.IsType<ComponentChildDTO>(snapshotField.GetValue(clipboard));
        Assert.Equal(source.FullName, snapshot.FullName);
        Assert.Equal("original", Assert.IsType<JsonElement>(snapshot.Properties.Single().Value).GetString());
        text = "Content copied in another application";
        await clipboard.RefreshAsync();
        Assert.Null(snapshotField.GetValue(clipboard));
    }

    [Fact]
    public void ComponentModel_ResolvesComponentTypesThroughInheritance()
    {
        var componentModel = InitializeResources().Get<IComponentModel>();
        var baseComponent = new Component
        {
            ClassName = nameof(Component),
            Namespace = typeof(Component).Namespace
        };
        var component = new Component
        {
            ClassName = "DerivedComponent",
            Namespace = "Example",
            BaseInheritedComponent = baseComponent
        };

        Assert.True(componentModel.IsOfType(component, typeof(Component)));
        Assert.False(componentModel.IsOfType(component, typeof(string)));
        Assert.False(componentModel.IsOfType(null, typeof(Component)));
        Assert.Equal(typeof(Component), componentModel.GetType(baseComponent));
    }

    [Fact]
    public void PropertyModel_RegisterReplacesAndFindsBaseTypeDefinitions()
    {
        var propertyModel = InitializeResources().Get<IPropertyModel>();
        var firstDefinition = new TestPropertyDefinition(typeof(PropertyApiBase));
        var replacement = new TestPropertyDefinition(typeof(PropertyApiBase));

        propertyModel.Register(firstDefinition);

        Assert.Same(firstDefinition, propertyModel.Get(typeof(PropertyApiBase)));
        Assert.Same(firstDefinition, propertyModel.Get(typeof(PropertyApiDerived)));
        Assert.Contains(firstDefinition, propertyModel.List());

        propertyModel.Register(replacement);
        Assert.Same(replacement, propertyModel.Get(typeof(PropertyApiDerived)));
        Assert.DoesNotContain(firstDefinition, propertyModel.List());

        propertyModel.Unregister(typeof(PropertyApiBase));
        Assert.Null(propertyModel.Get(typeof(PropertyApiDerived)));
        Assert.DoesNotContain(propertyModel.List(), definition => definition.Type == typeof(PropertyApiBase));
    }

    [Fact]
    public void PropertyModel_RegistrationActivatesAndRemovesJsonConverter()
    {
        var propertyModel = InitializeResources().Get<IPropertyModel>();
        var definition = new TestPropertyDefinition(typeof(ConvertedValue), new ConvertedValueConverter());

        propertyModel.Register(definition);
        try
        {
            var options = JSON.CreateOptions();
            var json = JsonSerializer.Serialize(new ConvertedValue("value"), options);

            Assert.Equal("\"value\"", json);
            Assert.Equal("value", JsonSerializer.Deserialize<ConvertedValue>(json, options).Value);
            Assert.Contains(options.Converters, converter => converter is ConvertedValueConverter);
        }
        finally
        {
            propertyModel.Unregister(typeof(ConvertedValue));
        }

        Assert.DoesNotContain(
            JSON.CreateOptions().Converters,
            converter => converter is ConvertedValueConverter);
    }

    [Fact]
    public void EditorCommandRegistry_ExecutesWithResolvedParametersAndCanExecute()
    {
        var registry = InitializeCommandRegistry();
        object executedParameter = null;
        var parameterProviderCalls = 0;

        registry.Register(
            "save",
            "Save",
            "Save the current document",
            "document",
            "Ctrl+S",
            parameter => executedParameter = parameter,
            parameter => Equals(parameter, "available"),
            () =>
            {
                parameterProviderCalls++;
                return "available";
            });

        Assert.False(registry.Execute("missing"));
        Assert.False(registry.Execute("save", "blocked"));
        Assert.True(registry.Execute("save"));
        Assert.Equal("available", executedParameter);
        Assert.Equal(1, parameterProviderCalls);
        Assert.Single(registry.Commands);
        Assert.Equal("save", registry.Commands.Single().Id);
    }

    [Fact]
    public void EditorCommandRegistry_MatchesShortcutByNormalizedScopeAndHonorsDisposal()
    {
        var registry = InitializeCommandRegistry();
        var executionCount = 0;
        var registration = registry.Register(
            "format",
            "Format",
            "Format document",
            "document",
            "Ctrl+K",
            _ => executionCount++);

        Assert.False(registry.TryExecuteShortcut("Control + K", "global"));
        Assert.True(registry.TryExecuteShortcut("Control + K", "DOCUMENT"));
        Assert.Equal(1, executionCount);

        registration.Dispose();
        Assert.False(registry.TryExecuteShortcut("Ctrl+K", "document"));
        Assert.Empty(registry.Commands);
    }

    [Fact]
    public void EditorCommandRegistry_DisposingReplacedRegistrationKeepsCurrentCommand()
    {
        var registry = InitializeCommandRegistry();
        var firstExecutionCount = 0;
        var secondExecutionCount = 0;
        var first = registry.Register("open", "Open", "", "global", "", _ => firstExecutionCount++);
        var second = registry.Register("open", "Open Again", "", "global", "", _ => secondExecutionCount++);

        first.Dispose();

        Assert.True(registry.Execute("open"));
        Assert.Equal(0, firstExecutionCount);
        Assert.Equal(1, secondExecutionCount);

        second.Dispose();
        Assert.False(registry.Execute("open"));
    }

    [Fact]
    public void EditorCommandRegistry_RegistersAndReturnsCustomCommand()
    {
        var registry = InitializeCommandRegistry();
        var command = new StringCommand();
        using var registration = registry.Register("custom", "Custom", "", "global", "", command);

        Assert.Same(command, registry.GetCommand<StringCommand>("custom"));
        Assert.False(registry.Execute("custom", 42));
        Assert.Null(command.LastValue);
        Assert.True(registry.Execute("custom", "value"));
        Assert.Equal("value", command.LastValue);
        Assert.Null(registry.GetCommand<StringCommand>("missing"));
    }

    [Fact]
    public void ContentProvider_RegistersAssociationsIconsAndRemovesThem()
    {
        var provider = InitializeResources().Get<IContentProvider>();

        provider.AddSupportedContentType<TrackedContent, EngineAsset, EngineTexture>(
            "texture",
            "texture.png",
            "Game.Scene",
            "Background");

        Assert.Equal("texture.png", provider.GetIcon(".TEXTURE"));
        var association = provider.GetAssociation("texture");
        Assert.NotNull(association);
        Assert.Equal(typeof(TrackedContent), association.EditorType);
        Assert.Equal(typeof(EngineAsset), association.EngineBaseType);
        Assert.Equal(typeof(EngineTexture), association.EngineImplementationType);
        Assert.Equal("Game.Scene", association.DefaultComponent);
        Assert.Equal("Background", association.DefaultProperty);
        Assert.Single(provider.ListAssociations());
        Assert.Single(provider.GetAssociationsForEngineType(typeof(EngineAsset)));

        provider.RemoveSupportedContentType<TrackedContent>("texture");

        Assert.Null(provider.GetIcon("texture"));
        Assert.Null(provider.GetAssociation("texture"));
        Assert.Empty(provider.ListAssociations());
    }

    [Fact]
    public async Task ContentProvider_TypeRegistrationReloadsUnchangedGenericContent()
    {
        var provider = InitializeResources().Get<IContentProvider>();
        var directory = CreateTempDirectory();
        var path = Path.Combine(directory, "preview.data");
        File.WriteAllBytes(path, [1, 2, 3]);
        var lastWrite = File.GetLastWriteTimeUtc(path);
        Task<Content> refreshed = null;
        Action changed = () => refreshed = provider.GetContentAsync(path);

        try
        {
            Assert.IsType<Content>(await provider.GetContentAsync(path));
            provider.ContentTypesChanged += changed;

            provider.AddSupportedContentType<TrackedContent>("data");

            Assert.NotNull(refreshed);
            Assert.True(Assert.IsType<TrackedContent>(await refreshed).Initialized);
            Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            provider.ContentTypesChanged -= changed;
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ContentProvider_NotifiesAfterCompleteRegistrationAndRemoval()
    {
        var provider = InitializeResources().Get<IContentProvider>();
        var notifications = 0;
        provider.ContentTypesChanged += () =>
        {
            notifications++;
            if (notifications == 1)
            {
                Assert.Equal("texture.png", provider.GetIcon("data"));
                Assert.NotNull(provider.GetAssociation("data"));
            }
            else
            {
                Assert.Null(provider.GetIcon("data"));
                Assert.Null(provider.GetAssociation("data"));
            }
        };

        provider.AddSupportedContentType<TrackedContent, EngineAsset, EngineTexture>("data", "texture.png");
        provider.RemoveSupportedContentType<TrackedContent>("data");
        provider.RemoveSupportedContentType<TrackedContent>("data");

        Assert.Equal(2, notifications);
    }

    [Fact]
    public async Task ContentProvider_InFlightLoadKeepsItsRegisteredContentType()
    {
        var provider = InitializeResources().Get<IContentProvider>();
        var directory = CreateTempDirectory();
        var path = Path.Combine(directory, "preview.data");
        File.WriteAllBytes(path, [1, 2, 3]);
        provider.AddSupportedContentType<TrackedContent>("data");

        try
        {
            var pending = provider.GetContentAsync(path);
            provider.RemoveSupportedContentType<TrackedContent>("data");

            Assert.True(Assert.IsType<TrackedContent>(await pending).Initialized);
            Assert.IsType<Content>(await provider.GetContentAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ContentProvider_LoadsRegisteredTypesAndFallsBackForUnknownExtensions()
    {
        var provider = InitializeResources().Get<IContentProvider>();
        var directory = CreateTempDirectory();
        var registeredPath = Path.Combine(directory, "registered.data");
        var fallbackPath = Path.Combine(directory, "unknown.bin");
        File.WriteAllBytes(registeredPath, [1, 2, 3]);
        File.WriteAllBytes(fallbackPath, [4, 5]);
        provider.AddSupportedContentType<TrackedContent>("data");

        try
        {
            var registered = await provider.GetContentAsync(registeredPath);
            var fallback = await provider.GetContentAsync(fallbackPath);

            var tracked = Assert.IsType<TrackedContent>(registered);
            Assert.Equal("registered", tracked.Name);
            Assert.Equal([1, 2, 3], tracked.Data);
            Assert.True(tracked.Initialized);
            Assert.IsType<Content>(fallback);
            Assert.Equal("unknown", fallback.Name);
            Assert.Equal([4, 5], fallback.Data);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static TestResourceModel InitializeResources()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        return resources;
    }

    private static IEditorCommandRegistry InitializeCommandRegistry()
    {
        var resources = InitializeResources();
        var registry = resources.Get<IEditorCommandRegistry>();
        registry.Init(resources);
        return registry;
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeDTest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class TestPropertyDefinition : IPropertyDefinition
    {
        public Type Type { get; }
        public Type ValueType => Type;
        public JsonConverter JsonConverter { get; }

        public TestPropertyDefinition(Type type, JsonConverter jsonConverter = null)
        {
            Type = type;
            JsonConverter = jsonConverter;
        }

        public string ToCode(object value) => value?.ToString();
        public PropertyNode CreateNode(Component component, Property property, Action<PropertyNode, object> valueChanged) => null;
        public IDataTemplate CreateTemplate() => null;
    }

    private sealed record ConvertedValue(string Value);

    private sealed class ConvertedValueConverter : JsonConverter<ConvertedValue>
    {
        public override ConvertedValue Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new ConvertedValue(reader.GetString());
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConvertedValue value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }

    private class PropertyApiBase
    {
    }

    private sealed class PropertyApiDerived : PropertyApiBase
    {
    }

    private sealed class StringCommand : CustomCommand<string>
    {
        public string LastValue { get; private set; }

        public override void Execute(string parameter)
        {
            LastValue = parameter;
        }
    }

    private class EngineAsset
    {
    }

    private sealed class EngineTexture : EngineAsset
    {
    }

    public sealed class TrackedContent : Content
    {
        public bool Initialized { get; private set; }

        public override void Initialize()
        {
            Initialized = true;
        }
    }
}

internal sealed class TestResourceModel : IResourceModel
{
    private readonly List<object> values = new();
    private readonly Dictionary<string, object> keyedValues = new();

    public void Init(IResourceModel resourceModel)
    {
    }

    public void Add(List<object> values)
    {
        this.values.AddRange(values);
    }

    public void Add(object value)
    {
        values.Add(value);
    }

    public void Add(string key, object value)
    {
        keyedValues[key] = value;
    }

    public void Add(List<Tuple<string, object>> keyValues)
    {
        foreach (var (key, value) in keyValues)
            keyedValues[key] = value;
    }

    public void Remove(string key)
    {
        keyedValues.Remove(key);
        values.RemoveAll(value => value.GetType().Name == key);
    }

    public T Get<T>(string key) where T : class
    {
        return keyedValues.TryGetValue(key, out var value) ? value as T : null;
    }

    public T Get<T>() where T : class
    {
        return values.OfType<T>().FirstOrDefault();
    }
}
