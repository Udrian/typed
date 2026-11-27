using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeD;
using TypeD.Code;
using TypeD.Components;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.DTO;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeDTest;

public class FormatVersionTests
{
    private static IFormatVersionModel CreateModel()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        return resources.Get<IFormatVersionModel>();
    }

    [Theory]
    [InlineData(FormatDocumentKind.Project)]
    [InlineData(FormatDocumentKind.Component)]
    public void UnversionedDocumentsUpgradeFromZeroWithoutChangingTheirInput(FormatDocumentKind kind)
    {
        var versions = CreateModel();
        var original = JsonNode.Parse("""{"Name":"Before","Nested":{"Value":42}}""").AsObject();
        var upgraded = versions.Upgrade(original, kind);

        Assert.False(original.ContainsKey("FormatVersions"));
        Assert.Equal(kind == FormatDocumentKind.Component ? 2 : 1, upgraded["FormatVersions"]["TypeD"].GetValue<int>());
        Assert.Equal(42, upgraded["Nested"]["Value"].GetValue<int>());
        Assert.True(JsonNode.DeepEquals(upgraded, versions.Upgrade(upgraded, kind)));
    }

    [Fact]
    public void ModuleConversionsRunConsecutivelyOnRawJsonBeforeDeserialization()
    {
        var versions = CreateModel();
        var calls = new List<int>();
        using var registration = versions.Register("ExampleModule", new FormatVersionDefinition(
            FormatDocumentKind.Component, 2, new Dictionary<int, Action<JsonObject>>
            {
                [0] = json =>
                {
                    calls.Add(0);
                    json["ClassName"] = json["OldName"]?.DeepClone();
                    json.Remove("OldName");
                },
                [1] = json =>
                {
                    calls.Add(1);
                    json["CustomProperties"] = new JsonArray();
                }
            }));

        var upgraded = versions.Upgrade(JsonNode.Parse("""{"OldName":"Player"}""").AsObject(), FormatDocumentKind.Component);
        var dto = upgraded.Deserialize<ComponentDTO>();

        Assert.Equal(new[] { 0, 1 }, calls);
        Assert.Equal("Player", dto.ClassName);
        Assert.Empty(dto.CustomProperties);
        Assert.Equal(2, dto.FormatVersions["ExampleModule"]);
        versions.Upgrade(upgraded, FormatDocumentKind.Component);
        Assert.Equal(2, calls.Count);
    }

    [Fact]
    public void FormatsAreIndependentAndOnlyNeededStepsRun()
    {
        var versions = CreateModel();
        using var component = versions.Register("Example", new FormatVersionDefinition(FormatDocumentKind.Component, 3,
            new Dictionary<int, Action<JsonObject>> { [2] = json => json["Converted"] = true }));
        using var project = versions.Register("Example", new FormatVersionDefinition(FormatDocumentKind.Project, 1,
            new Dictionary<int, Action<JsonObject>> { [0] = json => json["ProjectConverted"] = true }));
        var upgraded = versions.Upgrade(
            JsonNode.Parse("""{"FormatVersions":{"TypeD":1,"Example":2}}""").AsObject(), FormatDocumentKind.Component);

        Assert.True(upgraded["Converted"].GetValue<bool>());
        Assert.False(upgraded.ContainsKey("ProjectConverted"));
        Assert.Equal(3, upgraded["FormatVersions"]["Example"].GetValue<int>());
        var projectJson = versions.Upgrade(new JsonObject(), FormatDocumentKind.Project);
        Assert.True(projectJson["ProjectConverted"].GetValue<bool>());
        Assert.Equal(1, projectJson["FormatVersions"]["Example"].GetValue<int>());
    }

    [Fact]
    public void ComponentVersionOneUpgradePreservesNestedValues()
    {
        var input = JsonNode.Parse(
            """{"FormatVersions":{"TypeD":1},"Children":[{"FullName":"Game.Child","Properties":[{"Name":"Value","Value":42}]}]}""").AsObject();
        var upgraded = CreateModel().Upgrade(input, FormatDocumentKind.Component);
        Assert.Equal(2, upgraded["FormatVersions"]["TypeD"].GetValue<int>());
        Assert.Equal(42, upgraded["Children"][0]["Properties"][0]["Value"].GetValue<int>());
        Assert.Equal(1, input["FormatVersions"]["TypeD"].GetValue<int>());
    }

    [Theory]
    [InlineData("""{"FormatVersions":null}""")]
    [InlineData("""{"FormatVersions":1}""")]
    [InlineData("""{"FormatVersions":{"TypeD":-1}}""")]
    [InlineData("""{"FormatVersions":{"TypeD":"1"}}""")]
    [InlineData("""{"FormatVersions":{"TypeD":1.5}}""")]
    [InlineData("""{"FormatVersions":{"TypeD":3}}""")]
    [InlineData("""{"FormatVersions":{"MissingModule":1}}""")]
    public void InvalidOrUnsupportedVersionsAreRejected(string json)
    {
        Assert.Throws<InvalidDataException>(() =>
            CreateModel().Upgrade(JsonNode.Parse(json).AsObject(), FormatDocumentKind.Component));
    }

    [Fact]
    public void CoreOnlyBootstrapDefersModuleVersionsWithoutWriting()
    {
        var versions = CreateModel();
        using var directory = new TestDirectory();
        var path = directory.File("Game.typeo");
        var json = """{"ProjectName":"Game","FormatVersions":{"TypeD":1,"Example":5}}""";
        File.WriteAllText(path, json);

        var bootstrap = versions.Read<JsonObject>(path, FormatDocumentKind.Project, coreOnly: true);

        Assert.Equal("Game", bootstrap["ProjectName"].GetValue<string>());
        Assert.Equal(json, File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(directory.Path));
        Assert.Throws<InvalidDataException>(() => versions.Read<JsonObject>(path, FormatDocumentKind.Project));
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public void MissingStepsAndConverterFailuresDoNotMutateTheInput()
    {
        var versions = CreateModel();
        var input = new JsonObject { ["Original"] = true };
        using (versions.Register("MissingStep", new FormatVersionDefinition(FormatDocumentKind.Project, 2,
            new Dictionary<int, Action<JsonObject>> { [0] = json => json["Changed"] = true })))
        {
            var failure = Assert.Throws<InvalidDataException>(() => versions.Upgrade(input, FormatDocumentKind.Project));
            Assert.Contains("from version 1 to 2", failure.Message);
        }
        using (versions.Register("Throwing", new FormatVersionDefinition(FormatDocumentKind.Project, 1,
            new Dictionary<int, Action<JsonObject>>
            {
                [0] = json =>
                {
                    json["Changed"] = true;
                    throw new InvalidOperationException("Bad source data");
                }
            })))
        {
            var failure = Assert.Throws<InvalidDataException>(() => versions.Upgrade(input, FormatDocumentKind.Project));
            Assert.Contains("Throwing", failure.Message);
            Assert.IsType<InvalidOperationException>(failure.InnerException);
        }
        Assert.Single(input);
        Assert.True(input["Original"].GetValue<bool>());
    }

    [Fact]
    public void SuccessfulUpgradeBacksUpOriginalAndDoesNotRepeatOnNextRead()
    {
        var versions = CreateModel();
        using var directory = new TestDirectory();
        var path = directory.File("Player.component");
        var original = """{ "ClassName": "Player", "ModuleData": { "Value": 42 } }""";
        File.WriteAllText(path, original);

        var dto = versions.Read<ComponentDTO>(path, FormatDocumentKind.Component);

        Assert.Equal(2, dto.FormatVersions["TypeD"]);
        Assert.Equal(42, dto.ExtensionData["ModuleData"].GetProperty("Value").GetInt32());
        var backup = Assert.Single(Directory.GetFiles(directory.Path, "*.bak"));
        Assert.Equal(original, File.ReadAllText(backup));
        var persisted = File.ReadAllText(path);
        versions.Read<ComponentDTO>(path, FormatDocumentKind.Component);
        Assert.Equal(persisted, File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(directory.Path, "*.bak"));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void BatchUpgradePreflightsEveryFileBeforeWritingAny()
    {
        var versions = CreateModel();
        using var directory = new TestDirectory();
        var project = directory.File("Game.typeo");
        var component = directory.File("Player.component");
        File.WriteAllText(project, """{"ProjectName":"Game"}""");
        var future = """{"ClassName":"Player","FormatVersions":{"TypeD":999}}""";
        File.WriteAllText(component, future);
        var files = new[] { (project, FormatDocumentKind.Project), (component, FormatDocumentKind.Component) };

        Assert.Throws<InvalidDataException>(() => versions.UpgradeFiles(files));

        Assert.Equal("""{"ProjectName":"Game"}""", File.ReadAllText(project));
        Assert.Equal(future, File.ReadAllText(component));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.bak"));
        File.WriteAllText(component, """{"ClassName":"Player"}""");
        Assert.True(versions.UpgradeFiles(files));
        Assert.Equal(2, Directory.GetFiles(directory.Path, "*.bak").Length);
        Assert.False(versions.UpgradeFiles(files));
    }

    [Fact]
    public void ConversionAndDeserializationFailuresLeaveOriginalFilesUntouched()
    {
        var versions = CreateModel();
        using var directory = new TestDirectory();
        var path = directory.File("Broken.component");
        var original = """{"ClassName":42}""";
        File.WriteAllText(path, original);

        var error = Assert.Throws<InvalidDataException>(() => versions.Read<ComponentDTO>(path, FormatDocumentKind.Component));

        Assert.Contains(path, error.Message);
        Assert.Equal(original, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.bak"));
    }

    [Fact]
    public void UnsupportedFutureModuleAndThrowingConversionsDoNotWriteFiles()
    {
        var versions = CreateModel();
        using var module = versions.Register("Example", new FormatVersionDefinition(FormatDocumentKind.Component, 1,
            new Dictionary<int, Action<JsonObject>> { [0] = _ => throw new InvalidOperationException("Invalid old data") }));
        using var directory = new TestDirectory();
        var path = directory.File("Player.component");
        var future = """{"ClassName":"Player","FormatVersions":{"TypeD":1,"Example":2}}""";
        File.WriteAllText(path, future);

        var error = Assert.Throws<InvalidDataException>(() => versions.Read<ComponentDTO>(path, FormatDocumentKind.Component));
        Assert.Contains("Unsupported future", error.Message);
        Assert.Equal(future, File.ReadAllText(path));

        var legacy = """{"ClassName":"Player"}""";
        File.WriteAllText(path, legacy);
        error = Assert.Throws<InvalidDataException>(() => versions.Read<ComponentDTO>(path, FormatDocumentKind.Component));
        Assert.Contains("from version 0 to 1", error.Message);
        Assert.Equal(legacy, File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public void RegistrationIsUniqueScopedAndCanBeReplacedAfterUnload()
    {
        var versions = CreateModel();
        var definition = new FormatVersionDefinition(FormatDocumentKind.Component, 1,
            new Dictionary<int, Action<JsonObject>> { [0] = _ => { } });
        var registration = versions.Register("Example", definition);
        Assert.Throws<InvalidOperationException>(() => versions.Register("Example", definition));
        Assert.Equal(1, versions.GetCurrentVersions(FormatDocumentKind.Component)["Example"]);
        registration.Dispose();
        registration.Dispose();
        Assert.False(versions.GetCurrentVersions(FormatDocumentKind.Component).ContainsKey("Example"));
        using var replacement = versions.Register("Example", definition);
        Assert.Equal(1, versions.GetCurrentVersions(FormatDocumentKind.Component)["Example"]);
    }

    [Fact]
    public void ModuleLifecycleRegistersBeforeInitializationAndCleansUpAfterUnloadAndFailure()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var versions = resources.Get<IFormatVersionModel>();
        var model = resources.Get<IModuleModel>();
        model.Init(resources);
        var typeO = (ModuleTypeO)Activator.CreateInstance(typeof(ModuleTypeO), nonPublic: true);
        typeO.DevModuleName = "FormatVersionTests";
        var module = new TypeD.Models.Data.Module { Name = "LifecycleTest", Version = "0.1.0" };
        typeof(TypeD.Models.Data.Module).GetProperty(nameof(module.TypeO)).SetValue(module, typeO);
        typeof(TypeD.Models.Data.Module).GetProperty("DevAssembly", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(module, typeof(ConversionInitializer).Assembly);
        var initialize = model.GetType().GetMethod("InitializeTypeD", BindingFlags.Instance | BindingFlags.NonPublic);
        var uninitialize = model.GetType().GetMethod("UninitializeTypeD", BindingFlags.Instance | BindingFlags.NonPublic);

        initialize.Invoke(model, new object[] { null, module });
        Assert.Equal(1, versions.GetCurrentVersions(FormatDocumentKind.Component)["LifecycleTest"]);
        uninitialize.Invoke(model, new object[] { module });
        Assert.False(versions.GetCurrentVersions(FormatDocumentKind.Component).ContainsKey("LifecycleTest"));
        initialize.Invoke(model, new object[] { null, module });
        uninitialize.Invoke(model, new object[] { module });

        using var directory = new TestDirectory();
        var error = Assert.Throws<TargetInvocationException>(() =>
            initialize.Invoke(model, new object[] { CreateProject(directory.Path), module }));
        Assert.IsType<InvalidOperationException>(error.InnerException);
        Assert.False(versions.GetCurrentVersions(FormatDocumentKind.Component).ContainsKey("LifecycleTest"));
    }

    [Fact]
    public void SavingCannotSkipRequiredModuleConversions()
    {
        var versions = CreateModel();
        using var module = versions.Register("Example", new FormatVersionDefinition(
            FormatDocumentKind.Component, 2, new Dictionary<int, Action<JsonObject>>()));

        Assert.Throws<InvalidDataException>(() => versions.GetCurrentVersions(FormatDocumentKind.Component,
            new Dictionary<string, int> { ["TypeD"] = 1, ["Example"] = 1 }));
        Assert.Throws<InvalidDataException>(() => versions.GetCurrentVersions(FormatDocumentKind.Component,
            new Dictionary<string, int> { ["TypeD"] = 1 }));
        Assert.Equal(2, versions.GetCurrentVersions(FormatDocumentKind.Component)["Example"]);
    }

    [Fact]
    public async Task ProjectSaveStampsVersionsAndPreservesModuleFields()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var provider = resources.Get<IProjectProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var dataType = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var data = JsonSerializer.Deserialize("""{"ProjectName":"Game","ModuleData":{"Value":42}}""",
            dataType.GetParameters()[1].ParameterType);
        var project = (Project)dataType.Invoke(new[] { directory.Path, data });

        await provider.Save(project);

        var saved = JsonNode.Parse(File.ReadAllText(project.ProjectFilePath));
        Assert.Equal(1, saved["FormatVersions"]["TypeD"].GetValue<int>());
        Assert.Equal(42, saved["ModuleData"]["Value"].GetValue<int>());
    }

    [Fact]
    public async Task ComponentSaveStampsVersionsAndPreservesModuleFields()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var component = new Component
        {
            ClassName = "Player",
            Namespace = "Game",
            Template = new TestTemplate()
        };
        component.Properties.Add(new Property { Name = "Speed", Type = typeof(int), Value = 5 });
        component.OveriddenProperties.Add(new Property { Name = "Speed", Type = typeof(int), Value = 5 });
        var persisted = (HashSet<string>)typeof(Component).GetProperty("PersistedPropertyOverrides",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
        persisted.Add("Speed");
        component.RemovedCustomProperties.Add("Health");
        var extensions = new Dictionary<string, JsonElement>
        {
            ["ModuleData"] = JsonSerializer.SerializeToElement(new { Value = 42 })
        };
        typeof(Component).GetProperty("ExtensionData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(component, extensions);
        var save = new ComponentSaveContext();
        save.Init(resources, project);
        save.Components.Add(component);

        await save.SaveAction(project);

        var saved = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, component)));
        Assert.Equal(2, saved["FormatVersions"]["TypeD"].GetValue<int>());
        Assert.Equal(42, saved["ModuleData"]["Value"].GetValue<int>());
        Assert.Equal("Player", saved["ClassName"].GetValue<string>());
        Assert.Equal("Health", saved["RemovedCustomProperties"][0].GetValue<string>());
        Assert.Equal("Speed", saved["Properties"][0]["Name"].GetValue<string>());
        Assert.Equal(5, saved["Properties"][0]["Value"].GetValue<int>());
    }

    [Fact]
    public async Task LoadedComponentSave_PreservesExistingOverridesAndPendingDeletion()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        resources.Get<IHookModel>().AddHook<ExtractPropertiesHook>(hook =>
            hook.Properties.Add(new Property { Name = "Speed", Type = typeof(int), Value = 1 }));
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var path = provider.GetPath(project, "Game.Player");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonSerializer.Serialize(new ComponentDTO
        {
            ClassName = "Player",
            Namespace = "Game",
            TemplateClass = typeof(LoadableTestTemplate).FullName,
            BaseInheritedComponent = "",
            Interfaces = new List<string>(),
            Properties = new List<PropertyDTO> { new() { Name = "Speed", Value = 5 } },
            RemovedCustomProperties = new List<string> { "Health" }
        }));

        var component = provider.Load(project, "Game.Player");
        Assert.Equal(5, component.Properties.Single().Value);
        Assert.True(component.IsRemovedCustomProperty("Game.Player", "Health"));
        var context = resources.Get<ISaveModel>().GetSaveContext<ComponentSaveContext>(project);
        context.Components.Add(component);
        Assert.Same(component, provider.Load(project, "Game.Player"));

        await context.SaveAction(project);

        var saved = JsonNode.Parse(File.ReadAllText(path));
        Assert.Equal(5, saved["Properties"][0]["Value"].GetValue<int>());
        Assert.Equal("Health", saved["RemovedCustomProperties"][0].GetValue<string>());
    }

    [Fact]
    public async Task UnresolvedOverrides_SurviveSaveAndBecomeUsableWhenPropertiesReturn()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        var propertiesAvailable = false;
        resources.Get<IHookModel>().AddHook<ExtractPropertiesHook>(hook =>
        {
            if (propertiesAvailable)
                hook.Properties.Add(new Property { Name = "Former", Type = typeof(int), Value = 1 });
        });
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var childPath = provider.GetPath(project, "Game.Child");
        var rootPath = provider.GetPath(project, "Game.Root");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(rootPath));
        File.WriteAllText(childPath, JsonSerializer.Serialize(new ComponentDTO
        {
            ClassName = "Child", Namespace = "Game", TemplateClass = typeof(LoadableTestTemplate).FullName,
            BaseInheritedComponent = "", Interfaces = new List<string>()
        }));
        File.WriteAllText(rootPath, JsonSerializer.Serialize(new ComponentDTO
        {
            ClassName = "Root", Namespace = "Game", TemplateClass = typeof(LoadableTestTemplate).FullName,
            BaseInheritedComponent = "", Interfaces = new List<string>(),
            Properties = new List<PropertyDTO> { new() { Name = "Former", Value = 42 } },
            Children = new List<ComponentChildDTO>
            {
                new() { FullName = "Game.Child", Properties = new List<PropertyDTO> { new() { Name = "Former", Value = 99 } } }
            }
        }));

        var root = provider.Load(project, "Game.Root");
        Assert.Empty(root.Properties);
        Assert.True(Assert.Single(root.Children).HasChanges);
        Assert.Equal(99, Assert.IsType<JsonElement>(
            Assert.Single(provider.CaptureNode(root.Children[0]).Properties).Value).GetInt32());
        var context = resources.Get<ISaveModel>().GetSaveContext<ComponentSaveContext>(project);
        context.Components.Add(root);
        root.Properties.Add(new Property { Name = "Former", Type = typeof(int), Value = 3 });
        var collision = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveAction(project));
        Assert.Contains("Former", collision.Message);
        root.Properties.Clear();
        await context.SaveAction(project);
        var saved = JsonNode.Parse(File.ReadAllText(rootPath));
        Assert.Equal(42, saved["Properties"][0]["Value"].GetValue<int>());
        Assert.Equal(99, saved["Children"][0]["Properties"][0]["Value"].GetValue<int>());

        context.Components.Clear();
        propertiesAvailable = true;
        var reloaded = provider.Load(project, "Game.Root");
        Assert.Equal(42, Assert.Single(reloaded.Properties).Value);
        Assert.Equal(99, Assert.Single(Assert.Single(reloaded.Children).Properties).Value);
    }

    [Fact]
    public async Task ComponentCustomPropertyBatch_PreservesRemovedAndIncompatibleValuesAndSwapsNames()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        Component Actor(int first)
        {
            var actor = new Component
            {
                Namespace = "Game", ClassName = "Actor", Template = new LoadableTestTemplate()
            };
            foreach (var name in new[] { "First", "Second", "Removed", "Changed" })
            {
                actor.CustomProperties.Add(new CustomProperty { Name = name, TypeName = typeof(int).FullName, DefaultValue = 1 });
                actor.Properties.Add(new Property
                {
                    Name = name, Type = typeof(int), Value = name == "First" ? first : 8,
                    DeclaringTypeName = actor.FullName, FromComponent = actor
                });
                actor.OveriddenProperties.Add(new Property(actor.Properties.Last()) { Value = 1 });
            }
            return actor;
        }
        var definition = Actor(5);
        var instance = Actor(7);
        var owner = new Component { Namespace = "Game", ClassName = "Owner", Template = new LoadableTestTemplate() };
        owner.Children.Add(instance);
        var desired = new (string, CustomProperty, Type)[]
        {
            ("First", new CustomProperty { Name = "Second", TypeName = typeof(int).FullName, DefaultValue = 1 }, typeof(int)),
            ("Second", new CustomProperty { Name = "First", TypeName = typeof(int).FullName, DefaultValue = 1 }, typeof(int)),
            ("Changed", new CustomProperty { Name = "Changed", TypeName = typeof(string).FullName, DefaultValue = "new" }, typeof(string))
        };
        var plan = new ComponentCustomPropertyPlan(new[] { definition, owner }, definition.FullName, desired,
            resources.Get<IPropertyModel>());
        Assert.Equal(4, plan.PreservedValues.Count);
        plan.Apply();
        Assert.Equal(5, definition.Properties.Single(item => item.Name == "Second").Value);
        Assert.Equal(8, definition.Properties.Single(item => item.Name == "First").Value);
        Assert.Equal("new", definition.Properties.Single(item => item.Name == "Changed").Value);
        Assert.DoesNotContain(definition.Properties, item => item.Name == "Removed");
        var save = new ComponentSaveContext();
        save.Init(resources, project);
        save.Components.AddRange(new[] { definition, owner });
        await save.SaveAction(project);
        var saved = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, owner)));
        var archived = saved["Children"][0]["PreservedProperties"].AsArray();
        Assert.Equal(8, archived.Single(item => item["Name"].GetValue<string>() == "Removed")["Value"].GetValue<int>());
        Assert.Equal(8, archived.Single(item => item["Name"].GetValue<string>() == "Changed")["Value"].GetValue<int>());
        var reloaded = provider.Load(project, owner.FullName);
        var reloadedChild = Assert.Single(reloaded.Children);
        Assert.Equal(7, reloadedChild.Properties.Single(item => item.Name == "Second").Value);
        Assert.Equal(2, provider.CaptureNode(reloadedChild).PreservedProperties.Count);
        definition.Properties.Single(item => item.Name == "Second").Value = 100;
        Assert.Throws<InvalidOperationException>(plan.Undo);
        definition.Properties.Single(item => item.Name == "Second").Value = 5;
        plan.Undo();
        Assert.Equal(5, definition.Properties.Single(item => item.Name == "First").Value);
        Assert.Contains(definition.Properties, item => item.Name == "Removed");
        plan.Apply();
        Assert.Equal(7, instance.Properties.Single(item => item.Name == "Second").Value);
    }

    [Fact]
    public async Task ComponentRebase_PreservesValuesAndChildrenAcrossCopiesAndUndo()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var oldBase = new Component { Namespace = "Engine", ClassName = "Old", Template = new LoadableTestTemplate() };
        oldBase.Properties.AddRange(new[]
        {
            new Property { Name = "Speed", Type = typeof(int), Value = 1, DeclaringTypeName = oldBase.FullName },
            new Property { Name = "Former", Type = typeof(int), Value = 2, DeclaringTypeName = oldBase.FullName }
        });
        var newBase = new Component { Namespace = "Engine", ClassName = "New", Template = new LoadableTestTemplate() };
        newBase.Properties.AddRange(new[]
        {
            new Property { Name = "Speed", Type = typeof(int), Value = 3, DeclaringTypeName = newBase.FullName },
            new Property { Name = "Added", Type = typeof(string), Value = "new", DeclaringTypeName = newBase.FullName }
        });
        Component CreateActor(int speed, int former)
        {
            var actor = new Component
            {
                Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase,
                Template = new LoadableTestTemplate()
            };
            actor.Properties.AddRange(new[]
            {
                new Property(oldBase.Properties[0]) { Value = speed },
                new Property(oldBase.Properties[1]) { Value = former }
            });
            actor.OveriddenProperties.AddRange(oldBase.Properties.Select(property => new Property(property)));
            return actor;
        }
        var definition = CreateActor(8, 42);
        var instance = CreateActor(9, 99);
        var owner = new Component { Namespace = "Game", ClassName = "Owner", Template = new LoadableTestTemplate() };
        owner.Children.Add(instance);
        var derived = new Component
        {
            Namespace = "Game", ClassName = "Derived", BaseInheritedComponent = definition,
            Template = new LoadableTestTemplate(),
            Properties = definition.Properties.Select(property => new Property(property)).ToList(),
            OveriddenProperties = definition.OveriddenProperties.Select(property => new Property(property)).ToList()
        };
        var oldTemplate = definition.Template;
        var plan = new ComponentRebasePlan(project, resources.Get<IProjectModel>(), resources.Get<IPropertyModel>(),
            new[] { definition, owner, derived }, definition.FullName, newBase, Array.Empty<Type>());
        Assert.Same(oldBase, definition.BaseInheritedComponent);
        plan.Apply();
        Assert.Same(newBase, definition.BaseInheritedComponent);
        Assert.Same(newBase, instance.BaseInheritedComponent);
        Assert.Same(definition, derived.BaseInheritedComponent);
        Assert.Same(instance, Assert.Single(owner.Children));
        Assert.Equal(8, definition.Properties.Single(property => property.Name == "Speed").Value);
        Assert.Equal(9, instance.Properties.Single(property => property.Name == "Speed").Value);
        Assert.Equal("new", definition.Properties.Single(property => property.Name == "Added").Value);
        Assert.DoesNotContain(definition.Properties, property => property.Name == "Former");
        Assert.Equal(newBase.FullName, definition.Template.Code.BaseClass);
        var save = new ComponentSaveContext();
        save.Init(resources, project);
        save.Components.AddRange(new[] { definition, owner });
        await save.SaveAction(project);
        var savedDefinition = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, definition)));
        var savedOwner = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, owner)));
        Assert.Equal(42, savedDefinition["Properties"].AsArray()
            .Single(item => item["Name"].GetValue<string>() == "Former")["Value"].GetValue<int>());
        Assert.Equal(99, savedOwner["Children"][0]["Properties"].AsArray()
            .Single(item => item["Name"].GetValue<string>() == "Former")["Value"].GetValue<int>());
        plan.Undo();
        Assert.Same(oldBase, definition.BaseInheritedComponent);
        Assert.Same(oldTemplate, definition.Template);
        Assert.Equal(42, definition.Properties.Single(property => property.Name == "Former").Value);
        plan.Apply();
        Assert.Equal(8, definition.Properties.Single(property => property.Name == "Speed").Value);
        Assert.Contains(plan.PreservedOverrides, text => text.Contains("Former"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComponentRebase_RecognizesPropertiesDeclaredAboveTheComponentRoot(bool changeBase)
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        var oldBase = new Component { Namespace = "Engine", ClassName = "Entity", Template = new LoadableTestTemplate() };
        var inherited = new Property { Name = "Name", Type = typeof(string), Value = "default", DeclaringTypeName = "Engine.Object" };
        oldBase.Properties.Add(inherited);
        var newBase = changeBase
            ? new Component { Namespace = "Engine", ClassName = "Drawable", Template = new LoadableTestTemplate() }
            : oldBase;
        if (changeBase)
            newBase.Properties.Add(new Property(inherited) { Value = "new default" });
        var component = new Component
        {
            Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase, Template = new LoadableTestTemplate()
        };
        component.Properties.Add(new Property(inherited) { Value = "player" });
        component.OveriddenProperties.Add(new Property(inherited));
        var derived = new Component
        {
            Namespace = "Game", ClassName = "Enemy", BaseInheritedComponent = component, Template = new LoadableTestTemplate(),
            Properties = new List<Property> { new(inherited) { Value = "enemy" } },
            OveriddenProperties = new List<Property> { new(inherited) }
        };
        var plan = new ComponentRebasePlan(project, resources.Get<IProjectModel>(), resources.Get<IPropertyModel>(),
            new[] { component, derived }, component.FullName, newBase, Array.Empty<Type>());

        plan.Apply();
        Assert.Equal("player", Assert.Single(component.Properties).Value);
        Assert.Equal("enemy", Assert.Single(derived.Properties).Value);
        Assert.Empty(plan.PreservedOverrides);
        plan.Undo();
        Assert.Equal("player", Assert.Single(component.Properties).Value);
        plan.Apply();
        Assert.Equal("enemy", Assert.Single(derived.Properties).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComponentRebase_RejectsPropertyActuallyDeclaredOnComponent(bool customProperty)
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        var oldBase = new Component { Namespace = "Engine", ClassName = "Entity", Template = new LoadableTestTemplate() };
        oldBase.Properties.Add(new Property { Name = "Name", Type = typeof(string), DeclaringTypeName = "Engine.Object" });
        var component = new Component
        {
            Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase, Template = new LoadableTestTemplate()
        };
        component.Properties.Add(new Property
        {
            Name = "Name", Type = typeof(string),
            DeclaringTypeName = customProperty ? "Engine.Object" : component.FullName
        });
        if (customProperty)
            component.CustomProperties.Add(new CustomProperty { Name = "Name", TypeName = typeof(string).FullName });

        var error = Assert.Throws<InvalidOperationException>(() => new ComponentRebasePlan(project,
            resources.Get<IProjectModel>(), resources.Get<IPropertyModel>(), new[] { component }, component.FullName,
            oldBase, Array.Empty<Type>()));
        Assert.Contains("conflicts", error.Message);
    }

    [Fact]
    public async Task ComponentRebase_PreservesIncompatibleOverridesAndRejectsStaleState()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var oldBase = new Component { Namespace = "Engine", ClassName = "Old", Template = new LoadableTestTemplate() };
        oldBase.Properties.Add(new Property { Name = "Value", Type = typeof(int), Value = 1, DeclaringTypeName = oldBase.FullName });
        var next = new Component { Namespace = "Engine", ClassName = "Next", Template = new LoadableTestTemplate() };
        next.Properties.Add(new Property { Name = "Value", Type = typeof(string), Value = "new", DeclaringTypeName = next.FullName });
        var component = new Component
        {
            Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase,
            Template = new LoadableTestTemplate()
        };
        component.Properties.Add(new Property(oldBase.Properties[0]) { Value = 7 });
        component.OveriddenProperties.Add(new Property(oldBase.Properties[0]));
        var persisted = (HashSet<string>)typeof(Component).GetProperty("PersistedPropertyOverrides",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
        persisted.Add("Value");
        var model = resources.Get<IProjectModel>();
        var properties = resources.Get<IPropertyModel>();
        var plan = new ComponentRebasePlan(project, model, properties, new[] { component }, component.FullName, next, Array.Empty<Type>());
        Assert.Equal(7, component.Properties[0].Value);
        component.Properties[0].Value = 8;
        Assert.Throws<InvalidOperationException>(() => plan.Apply());
        Assert.Same(oldBase, component.BaseInheritedComponent);
        component.Properties[0].Value = 7;
        plan.Apply();
        Assert.Equal("new", component.Properties[0].Value);
        Assert.Contains(plan.PreservedOverrides, value => value.Contains("Value"));
        var save = new ComponentSaveContext();
        save.Init(resources, project);
        save.Components.Add(component);
        await save.SaveAction(project);
        var file = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, component)));
        Assert.Equal(7, file["PreservedProperties"][0]["Value"].GetValue<int>());
        Assert.Equal(typeof(int).FullName, file["PreservedProperties"][0]["TypeName"].GetValue<string>());
        Assert.Empty(file["Properties"].AsArray());
        provider.AddBaseTypeComponent(next);
        var oldAssembly = true;
        resources.Get<IHookModel>().AddHook<ExtractPropertiesHook>(hook =>
        {
            if (hook.Component.FullName == component.FullName)
                hook.Properties.Add(oldAssembly
                    ? new Property { Name = "Value", Type = typeof(int), Value = 1, DeclaringTypeName = oldBase.FullName }
                    : new Property { Name = "Value", Type = typeof(string), Value = "new", DeclaringTypeName = next.FullName });
        });
        Assert.Equal(1, provider.Load(project, component.FullName).Properties.Single().Value);
        oldAssembly = false;
        var loaded = provider.Load(project, component.FullName);
        Assert.Equal("new", loaded.Properties.Single().Value);
        Assert.Equal(7, Assert.IsType<JsonElement>(Assert.Single(provider.CaptureNode(loaded).PreservedProperties).Value).GetInt32());
        var clone = provider.CreateInstance(project, loaded);
        Assert.Equal(7, Assert.IsType<JsonElement>(Assert.Single(provider.CaptureNode(clone).PreservedProperties).Value).GetInt32());
        plan.Undo();
        Assert.Equal(7, component.Properties[0].Value);
    }

    [Fact]
    public async Task ComponentRebase_PreservesIncompatibleValuesOnChildInstances()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var oldBase = new Component { Namespace = "Engine", ClassName = "Old", Template = new LoadableTestTemplate() };
        oldBase.Properties.Add(new Property { Name = "Value", Type = typeof(int), Value = 1, DeclaringTypeName = oldBase.FullName });
        var next = new Component { Namespace = "Engine", ClassName = "Next", Template = new LoadableTestTemplate() };
        next.Properties.Add(new Property { Name = "Value", Type = typeof(string), Value = "default", DeclaringTypeName = next.FullName });
        Component Actor(int value)
        {
            var actor = new Component { Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase, Template = new LoadableTestTemplate() };
            actor.Properties.Add(new Property(oldBase.Properties[0]) { Value = value });
            actor.OveriddenProperties.Add(new Property(oldBase.Properties[0]));
            return actor;
        }
        var definition = Actor(2);
        var instance = Actor(55);
        var owner = new Component { Namespace = "Game", ClassName = "Owner", Template = new LoadableTestTemplate() };
        owner.Children.Add(instance);
        var plan = new ComponentRebasePlan(project, resources.Get<IProjectModel>(), resources.Get<IPropertyModel>(),
            new[] { definition, owner }, definition.FullName, next, Array.Empty<Type>());
        plan.Apply();
        var save = new ComponentSaveContext();
        save.Init(resources, project);
        save.Components.AddRange(new[] { definition, owner });
        await save.SaveAction(project);
        var json = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, owner)));
        Assert.Equal(55, json["Children"][0]["PreservedProperties"][0]["Value"].GetValue<int>());

        provider.AddBaseTypeComponent(next);
        resources.Get<IHookModel>().AddHook<ExtractPropertiesHook>(hook =>
        {
            if (hook.Component.FullName == definition.FullName)
                hook.Properties.Add(new Property { Name = "Value", Type = typeof(string), Value = "default", DeclaringTypeName = next.FullName });
        });
        var loaded = provider.Load(project, owner.FullName);
        var copied = provider.CaptureNode(Assert.Single(loaded.Children));
        Assert.Equal(55, Assert.IsType<JsonElement>(Assert.Single(copied.PreservedProperties).Value).GetInt32());
        var restored = provider.RestoreNode(project, copied);
        Assert.Equal(55, Assert.IsType<JsonElement>(Assert.Single(provider.CaptureNode(restored).PreservedProperties).Value).GetInt32());
    }

    [Fact]
    public void ComponentRebase_RestoresAllStateWhenTemplateInitializationFails()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        var oldBase = new Component { Namespace = "Engine", ClassName = "Old", Template = new LoadableTestTemplate() };
        var failingBase = new Component { Namespace = "Engine", ClassName = "Broken", Template = new FailingRebaseTemplate() };
        var component = new Component { Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase, Template = new LoadableTestTemplate() };
        var template = component.Template;
        var properties = component.Properties;
        var plan = new ComponentRebasePlan(project, resources.Get<IProjectModel>(), resources.Get<IPropertyModel>(),
            new[] { component }, component.FullName, failingBase, Array.Empty<Type>());
        Assert.Throws<InvalidOperationException>(() => plan.Apply());
        Assert.Same(oldBase, component.BaseInheritedComponent);
        Assert.Same(template, component.Template);
        Assert.Same(properties, component.Properties);
    }

    [Fact]
    public void ComponentRebase_RejectsCyclesAndChangesToBaseOrAppliedValues()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        var oldBase = new Component { Namespace = "Engine", ClassName = "Old", Template = new LoadableTestTemplate() };
        var next = new Component { Namespace = "Engine", ClassName = "Next", Template = new LoadableTestTemplate() };
        next.Properties.Add(new Property { Name = "Value", Type = typeof(int), Value = 1, DeclaringTypeName = next.FullName });
        var component = new Component { Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = oldBase, Template = new LoadableTestTemplate() };
        var derived = new Component { Namespace = "Game", ClassName = "Derived", BaseInheritedComponent = component, Template = new LoadableTestTemplate() };
        var model = resources.Get<IProjectModel>();
        var properties = resources.Get<IPropertyModel>();
        Assert.Throws<InvalidOperationException>(() =>
            new ComponentRebasePlan(project, model, properties, new[] { component, derived }, component.FullName, derived, Array.Empty<Type>()));
        var plan = new ComponentRebasePlan(project, model, properties, new[] { component }, component.FullName, next, Array.Empty<Type>());
        next.Properties[0].Value = 2;
        Assert.Throws<InvalidOperationException>(() => plan.Apply());
        next.Properties[0].Value = 1;
        plan.Apply();
        component.Properties.Single().Value = 9;
        Assert.Throws<InvalidOperationException>(() => plan.Undo());
        Assert.Same(next, component.BaseInheritedComponent);
    }

    [Fact]
    public async Task RenamedPersistedOverride_IsSavedUnderTheNewName()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        resources.Get<IHookModel>().AddHook<ExtractPropertiesHook>(hook =>
            hook.Properties.Add(new Property { Name = "Speed", Type = typeof(int), Value = 1 }));
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var path = provider.GetPath(project, "Game.Player");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonSerializer.Serialize(new ComponentDTO
        {
            ClassName = "Player",
            Namespace = "Game",
            TemplateClass = typeof(LoadableTestTemplate).FullName,
            BaseInheritedComponent = "",
            Interfaces = new List<string>(),
            Properties = new List<PropertyDTO> { new() { Name = "Speed", Value = 5 } }
        }));
        var component = provider.Load(project, "Game.Player");
        component.RenamePersistedPropertyOverride("Speed", "Velocity");
        component.Properties.Single().Name = "Velocity";
        component.OveriddenProperties.Single().Name = "Velocity";
        var context = resources.Get<ISaveModel>().GetSaveContext<ComponentSaveContext>(project);
        context.Components.Add(component);

        await context.SaveAction(project);

        var saved = JsonNode.Parse(File.ReadAllText(path));
        Assert.Equal("Velocity", saved["Properties"][0]["Name"].GetValue<string>());
        Assert.Equal(5, saved["Properties"][0]["Value"].GetValue<int>());
    }

    [Fact]
    public async Task CreatedComponent_InitializesAndSavesDraftPropertiesWithInheritedProperties()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        project.IsClosing = true;
        var parent = new Component { Namespace = "Engine", ClassName = "Base", Template = new LoadableTestTemplate() };
        parent.CustomProperties.Add(new CustomProperty { Name = "Inherited", TypeName = typeof(int).FullName, DefaultValue = 3 });
        provider.AddBaseTypeComponent(parent);
        var sawDraftDuringTreeBuild = false;
        resources.Get<IHookModel>().AddHook<ComponentTreeBuiltHook>(hook =>
            sawDraftDuringTreeBuild = provider.ListAll(project).Single().CustomProperties.Count == 2);

        var template = provider.Create(project, "Player", "Game", parent, null, null, new List<CustomProperty>
        {
            new() { Name = "Health", TypeName = typeof(int).FullName, DefaultValue = 42 },
            new() { Name = "Secret", TypeName = typeof(int).FullName, DefaultValue = 7, Access = CustomPropertyAccess.Private }
        });
        var component = template.Component;
        Assert.True(sawDraftDuringTreeBuild);
        Assert.Equal(42, component.Properties.Single(property => property.Name == "Health").Value);
        Assert.Equal(3, component.Properties.Single(property => property.Name == "Inherited").Value);
        Assert.DoesNotContain(component.Properties, property => property.Name == "Secret");
        Assert.Contains(component.OveriddenProperties, property => property.Name == "Health");
        template.Code.Generate();
        Assert.Contains("public int Health", ((LoadableTestCode)template.Code).GeneratedOutput);
        Assert.Contains("private int Secret", ((LoadableTestCode)template.Code).GeneratedOutput);

        var save = resources.Get<ISaveModel>().GetSaveContext<ComponentSaveContext>(project);
        await save.SaveAction(project);
        var saved = JsonNode.Parse(File.ReadAllText(provider.GetPath(project, component)));
        Assert.Equal(2, saved["CustomProperties"].AsArray().Count);
        Assert.Equal(42, saved["CustomProperties"][0]["DefaultValue"].GetValue<int>());
        save.Components.Remove(component);
        var reloaded = provider.Load(project, component.FullName);
        Assert.Equal(42, Assert.IsType<int>(reloaded.CustomProperties[0].DefaultValue));
        Assert.Equal(42, reloaded.Properties.Single(property => property.Name == "Health").Value);
        Assert.Equal(3, reloaded.Properties.Single(property => property.Name == "Inherited").Value);
        Assert.Equal(CustomPropertyAccess.Private, reloaded.CustomProperties[1].Access);
    }

    [Fact]
    public void ReaddedCustomProperty_UsesCurrentTypeBeforeReadingSavedValue()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        var provider = resources.Get<IComponentProvider>();
        provider.Init(resources);
        resources.Get<IHookModel>().AddHook<ExtractPropertiesHook>(hook =>
            hook.Properties.Add(new Property
            {
                Name = "Health", Type = typeof(int), Value = 1, DeclaringTypeName = "Game.Player"
            }));
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        var path = provider.GetPath(project, "Game.Player");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonSerializer.Serialize(new ComponentDTO
        {
            ClassName = "Player",
            Namespace = "Game",
            TemplateClass = typeof(LoadableTestTemplate).FullName,
            BaseInheritedComponent = "",
            Interfaces = new List<string>(),
            Properties = new List<PropertyDTO> { new() { Name = "Health", Value = "ready" } },
            CustomProperties = new List<CustomPropertyDTO>
            {
                new() { Name = "Health", Type = typeof(string).FullName, DefaultValue = "new", Group = "Stats", Access = "Public" }
            },
            RemovedCustomProperties = new List<string> { "Health" }
        }));

        var property = Assert.Single(provider.Load(project, "Game.Player").Properties);

        Assert.Equal(typeof(string), property.Type);
        Assert.Equal("ready", property.Value);
        Assert.Equal("Stats", property.Group);
    }

    [Fact]
    public void ProjectUpgradeRunsModuleConvertersAfterBootstrapAndUpdatesLoadedProject()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var formats = resources.Get<IFormatVersionModel>();
        using var module = formats.Register("Example", new FormatVersionDefinition(FormatDocumentKind.Project, 1,
            new Dictionary<int, Action<JsonObject>> { [0] = json => json["StartScene"] = "Game.NewScene" }));
        var provider = resources.Get<IProjectProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        File.WriteAllText(project.ProjectFilePath,
            """{"ProjectName":"Game","CSSolutionPath":"Game.sln","CSProjName":"Game","StartScene":"Game.OldScene"}""");
        var components = System.IO.Path.Combine(project.ProjectTypeOPath, "components");
        Directory.CreateDirectory(components);
        var componentFile = System.IO.Path.Combine(components, "Player.component");
        File.WriteAllText(componentFile, """{"ClassName":"Player"}""");

        var upgrade = provider.GetType().GetMethod("UpgradeFormats", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True((bool)upgrade.Invoke(provider, new object[] { project, project.ProjectFilePath }));

        Assert.Equal("Game.NewScene", project.StartScene);
        Assert.Equal(2, JsonNode.Parse(File.ReadAllText(componentFile))["FormatVersions"]["TypeD"].GetValue<int>());
        Assert.Single(Directory.GetFiles(directory.Path, "*.bak"));
        Assert.Single(Directory.GetFiles(components, "*.bak"));
        Assert.False((bool)upgrade.Invoke(provider, new object[] { project, project.ProjectFilePath }));
    }

    [Fact]
    public void ProjectModuleConvertersCannotChangeDependencyBootstrapFields()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        using var module = resources.Get<IFormatVersionModel>().Register("Example", new FormatVersionDefinition(
            FormatDocumentKind.Project, 1, new Dictionary<int, Action<JsonObject>>
            {
                [0] = json => json["ProjectName"] = "Other"
            }));
        var provider = resources.Get<IProjectProvider>();
        provider.Init(resources);
        using var directory = new TestDirectory();
        var project = CreateProject(directory.Path);
        var original = """{"ProjectName":"Game","CSSolutionPath":"Game.sln","CSProjName":"Game"}""";
        File.WriteAllText(project.ProjectFilePath, original);

        var upgrade = provider.GetType().GetMethod("UpgradeFormats", BindingFlags.Instance | BindingFlags.NonPublic);
        var error = Assert.Throws<TargetInvocationException>(() =>
            upgrade.Invoke(provider, new object[] { project, project.ProjectFilePath }));

        Assert.IsType<InvalidDataException>(error.InnerException);
        Assert.Equal(original, File.ReadAllText(project.ProjectFilePath));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.bak"));
    }

    [Fact]
    public void GeneratedFilesContainVersionsButUserFilesDoNot()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        using var module = resources.Get<IFormatVersionModel>().Register("Example", new FormatVersionDefinition(
            FormatDocumentKind.Component, 2, new Dictionary<int, Action<JsonObject>>()));
        var code = new TestCode(new Component { ClassName = "Player", Namespace = "Game" });
        typeof(Codalyzer).GetProperty(nameof(Codalyzer.Resources)).SetValue(code, resources);

        code.Generate();

        Assert.Contains("""// TypeO format versions: {"TypeD":2,"Example":2}""", code.GeneratedOutput);
        Assert.DoesNotContain("TypeO format versions:", code.UserOutput);
    }

    [Fact]
    public void ProgramCodeUsesProjectFormatVersions()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        using var module = resources.Get<IFormatVersionModel>().Register("Example", new FormatVersionDefinition(
            FormatDocumentKind.Project, 3, new Dictionary<int, Action<JsonObject>>()));
        using var directory = new TestDirectory();
        var code = new TestProgramCode();
        typeof(Codalyzer).GetProperty(nameof(Codalyzer.Resources)).SetValue(code, resources);
        typeof(Codalyzer).GetProperty(nameof(Codalyzer.Project)).SetValue(code, CreateProject(directory.Path));
        code.Init();

        code.Generate();

        Assert.Contains("""// TypeO format versions: {"TypeD":1,"Example":3}""", code.Output);
    }

    private sealed class TestCode(Component component) : ComponentTypeCode(component)
    {
        public override Type TypeOBaseType => typeof(object);
        protected override void InitClass() { }
        protected override void InitTypeDClass() { }
        public string GeneratedOutput => TypeDFile.Output.ToString();
        public string UserOutput => BaseFile.Output.ToString();
    }

    private sealed class TestTemplate : ComponentTemplate<TestCode>
    {
        public override void ChildrenFilter(FilterHelper filter) { }
    }

    public sealed class LoadableTestCode(Component component) : ComponentTypeCode(component)
    {
        public override Type TypeOBaseType => typeof(object);
        protected override void InitClass() { }
        protected override void InitTypeDClass() { }
        public string GeneratedOutput => TypeDFile.Output.ToString();
    }

    public sealed class LoadableTestTemplate : ComponentTemplate<LoadableTestCode>
    {
        public override void ChildrenFilter(FilterHelper filter) { }
    }

    public sealed class FailingRebaseTemplate : ComponentTemplate<LoadableTestCode>
    {
        public override void Init() => throw new InvalidOperationException("Template failed.");
        public override void ChildrenFilter(FilterHelper filter) { }
    }

    private sealed class TestProgramCode : ProgramCode
    {
        public string Output => BaseFile.Output.ToString();
    }

    public sealed class ConversionInitializer : TypeDModuleInitializer
    {
        public override IEnumerable<FormatVersionDefinition> FormatVersions => new[]
        {
            new FormatVersionDefinition(FormatDocumentKind.Component, 1,
                new Dictionary<int, Action<JsonObject>> { [0] = _ => { } })
        };

        public override void Initializer(Project project)
        {
            Assert.Equal(1, Resources.Get<IFormatVersionModel>().GetCurrentVersions(FormatDocumentKind.Component)["LifecycleTest"]);
            if (project != null)
                throw new InvalidOperationException("Initializer failed");
        }

        public override void Uninitializer() { }
    }

    private static Project CreateProject(string directory)
    {
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var data = JsonSerializer.Deserialize(
            """{"ProjectName":"Game","CSSolutionPath":"Game.sln","CSProjName":"Game"}""",
            constructor.GetParameters()[1].ParameterType);
        return (Project)constructor.Invoke(new[] { directory, data });
    }

    private sealed class TestDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TypeOFormats-{Guid.NewGuid():N}");
        public TestDirectory() => Directory.CreateDirectory(Path);
        public string File(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
