using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeD;
using TypeD.Code;
using TypeD.Components;
using TypeD.Helpers;
using TypeD.Models.Data;
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
        Assert.Equal(1, upgraded["FormatVersions"]["TypeD"].GetValue<int>());
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

    [Theory]
    [InlineData("""{"FormatVersions":null}""")]
    [InlineData("""{"FormatVersions":1}""")]
    [InlineData("""{"FormatVersions":{"TypeD":-1}}""")]
    [InlineData("""{"FormatVersions":{"TypeD":"1"}}""")]
    [InlineData("""{"FormatVersions":{"TypeD":1.5}}""")]
    [InlineData("""{"FormatVersions":{"TypeD":2}}""")]
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

        Assert.Equal(1, dto.FormatVersions["TypeD"]);
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
        Assert.Equal(1, saved["FormatVersions"]["TypeD"].GetValue<int>());
        Assert.Equal(42, saved["ModuleData"]["Value"].GetValue<int>());
        Assert.Equal("Player", saved["ClassName"].GetValue<string>());
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
        Assert.Equal(1, JsonNode.Parse(File.ReadAllText(componentFile))["FormatVersions"]["TypeD"].GetValue<int>());
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

        Assert.Contains("""// TypeO format versions: {"TypeD":1,"Example":2}""", code.GeneratedOutput);
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
