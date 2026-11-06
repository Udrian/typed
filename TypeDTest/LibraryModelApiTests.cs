using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls.Templates;
using TypeD;
using TypeD.Commands;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using TypeD.ViewModel;

namespace TypeDTest;

public class LibraryModelApiTests
{
    [Fact]
    public void Init_RegistersBuiltInModelAndProviderInterfaces()
    {
        var resources = InitializeResources();

        Assert.NotNull(resources.Get<IComponentModel>());
        Assert.NotNull(resources.Get<IPropertyModel>());
        Assert.NotNull(resources.Get<IEditorCommandRegistry>());
        Assert.NotNull(resources.Get<IContentProvider>());
        Assert.Same(resources, ViewModelBase.ResourceModel);
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
        Assert.Empty(propertyModel.List());
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
