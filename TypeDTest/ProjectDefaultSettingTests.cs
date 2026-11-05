using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Data.SettingContexts;
using TypeD.Models.Interfaces;

namespace TypeDTest;

public class ProjectDefaultSettingTests
{
    [Fact]
    public async Task SingleDefaultIsSparsePersistsAndAllowsUserFactoryValueToOverrideIt()
    {
        using var fixture = new Fixture();
        var context = fixture.Settings.GetContext<TestContext>();
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, 42);
        Assert.True(fixture.Settings.HasProjectDefault(context.Count));
        Assert.Equal(42, fixture.Settings.GetRevertValue(context.Count));
        var json = JsonNode.Parse(File.ReadAllText(fixture.ProjectFile));
        Assert.Equal(42, json["Count"]["Value"].GetValue<int>());
        Assert.False(json.AsObject().ContainsKey("Text"));

        context = fixture.Settings.GetContext<TestContext>();
        context.Count.Value = 5; // The declared default must still override a project value.
        fixture.Settings.SetContext(context);
        Assert.Equal(5, fixture.Settings.GetContext<TestContext>().Count.Value);
        Assert.True(fixture.Settings.HasUserOverride(context.Count));
        await fixture.SaveUser();
        fixture.Settings.InitContext<TestContext>(fixture.Project);
        Assert.Equal(5, fixture.Settings.GetContext<TestContext>().Count.Value);

        fixture.Settings.ClearUserOverride(fixture.Settings.GetContext<TestContext>().Count);
        Assert.Equal(42, fixture.Settings.GetContext<TestContext>().Count.Value);
        await fixture.SaveUser();
        fixture.Settings.InitContext<TestContext>(fixture.Project);
        Assert.Equal(42, fixture.Settings.GetContext<TestContext>().Count.Value);
    }

    [Fact]
    public async Task EditingOneSettingDoesNotFreezeOtherInheritedValues()
    {
        using var fixture = new Fixture();
        var context = fixture.Settings.GetContext<TestContext>();
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, 42);
        context = fixture.Settings.GetContext<TestContext>();
        context.Text.Value = "User text";
        fixture.Settings.SetContext(context);
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, 77);
        Assert.Equal(77, fixture.Settings.GetContext<TestContext>().Count.Value);
        Assert.Equal("User text", fixture.Settings.GetContext<TestContext>().Text.Value);
        Assert.False(fixture.Settings.HasUserOverride(context.Count));
        Assert.True(fixture.Settings.HasUserOverride(context.Text));
    }

    [Fact]
    public async Task RemovingDefaultPreservesUserOverridesAndOtherProjectDefaults()
    {
        using var fixture = new Fixture();
        var context = fixture.Settings.GetContext<TestContext>();
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, 42);
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Text, "Shared");
        context = fixture.Settings.GetContext<TestContext>();
        context.Count.Value = 99;
        fixture.Settings.SetContext(context);

        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, null, remove: true);

        Assert.False(fixture.Settings.HasProjectDefault(context.Count));
        Assert.Equal(99, fixture.Settings.GetContext<TestContext>().Count.Value);
        Assert.Equal("Shared", fixture.Settings.GetContext<TestContext>().Text.Value);
        Assert.Equal(5, fixture.Settings.GetRevertValue(context.Count));
        fixture.Settings.ClearUserOverride(context.Count);
        Assert.Equal(5, fixture.Settings.GetContext<TestContext>().Count.Value);
        await fixture.SaveUser();
        fixture.Settings.InitContext<TestContext>(fixture.Project);
        Assert.Equal(5, fixture.Settings.GetContext<TestContext>().Count.Value);
        Assert.Equal("Shared", fixture.Settings.GetContext<TestContext>().Text.Value);
        Assert.False(JsonNode.Parse(File.ReadAllText(fixture.ProjectFile)).AsObject().ContainsKey("Count"));
    }

    [Fact]
    public async Task ExplicitFactoryAndNullDefaultsPersistAndReload()
    {
        using var fixture = new Fixture();
        var context = fixture.Settings.GetContext<TestContext>();
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, 5);
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Optional, null);
        fixture.Settings.InitContext<TestContext>(fixture.Project);
        context = fixture.Settings.GetContext<TestContext>();
        Assert.True(fixture.Settings.HasProjectDefault(context.Count));
        Assert.Equal(5, context.Count.Value);
        Assert.True(fixture.Settings.HasProjectDefault(context.Optional));
        Assert.Null(context.Optional.Value);
        Assert.Null(fixture.Settings.GetRevertValue(context.Optional));
    }

    [Fact]
    public async Task IndividualShortcutsMergeAcrossLevelsAndRemoveIndependently()
    {
        using var fixture = new Fixture();
        var context = fixture.Settings.GetContext<TestContext>();
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Shortcuts, "Ctrl+J", entry: "one");
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Shortcuts, "Ctrl+K", entry: "two");
        context = fixture.Settings.GetContext<TestContext>();
        context.Shortcuts.Value["one"] = "Ctrl+L";
        fixture.Settings.SetContext(context);
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Shortcuts, "Ctrl+M", entry: "two");
        var effective = fixture.Settings.GetContext<TestContext>();
        Assert.Equal("Ctrl+L", effective.Shortcuts.Value["one"]);
        Assert.Equal("Ctrl+M", effective.Shortcuts.Value["two"]);
        Assert.True(fixture.Settings.HasUserOverride(context.Shortcuts, "one"));
        Assert.False(fixture.Settings.HasUserOverride(context.Shortcuts, "two"));
        fixture.Settings.ClearUserOverride(context.Shortcuts, "one");
        Assert.Equal("Ctrl+J", fixture.Settings.GetContext<TestContext>().Shortcuts.Value["one"]);
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Shortcuts, null, remove: true, entry: "one");
        Assert.False(fixture.Settings.HasProjectDefault(context.Shortcuts, "one"));
        Assert.Equal("Ctrl+M", fixture.Settings.GetContext<TestContext>().Shortcuts.Value["two"]);
        await fixture.SaveUser();
        fixture.Settings.InitContext<TestContext>(fixture.Project);
        effective = fixture.Settings.GetContext<TestContext>();
        Assert.False(effective.Shortcuts.Value.ContainsKey("one"));
        Assert.Equal("Ctrl+M", effective.Shortcuts.Value["two"]);
    }

    [Fact]
    public async Task RevertFallsBackToSystemScopeAfterProjectDefaultIsRemoved()
    {
        using var fixture = new Fixture();
        fixture.Settings.SetContext(new TestContext { Count = new Setting<int>(5) { Value = 17 } }, SettingLevel.System);
        var context = fixture.Settings.GetContext<TestContext>();
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, 42);
        Assert.Equal(42, fixture.Settings.GetRevertValue(context.Count));
        await fixture.Settings.SetProjectDefaultAsync(fixture.Project, context.Count, null, remove: true);
        Assert.Equal(17, fixture.Settings.GetRevertValue(context.Count));
        Assert.Equal(17, fixture.Settings.GetContext<TestContext>().Count.Value);
    }

    [Name("ProjectDefaultTests-d26fd3")]
    public class TestContext : SettingContext
    {
        public Setting<int> Count { get; set; } = new(5);
        public Setting<string> Text { get; set; } = new("Original");
        public Setting<string> Optional { get; set; } = new("Not null");
        public Setting<ShortcutBindingOverrides> Shortcuts { get; set; } = new(new());
        public TestContext() { SaveOnExit = true; }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string Folder = Path.Combine(Path.GetTempPath(), "ProjectDefaultTests-" + Guid.NewGuid());
        private readonly TestResourceModel Resources = new();
        public ISettingModel Settings { get; }
        public Project Project { get; }
        public string ProjectFile => Path.Combine(Settings.GetPath(Project, SettingLevel.Project), Settings.GetName<TestContext>() + ".json");

        public Fixture()
        {
            TypeDInit.Init(Resources);
            Settings = Resources.Get<ISettingModel>();
            Settings.Init(Resources);
            var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var dto = JsonSerializer.Deserialize($$"""{"ProjectName":"ProjectDefaultTests-{{Guid.NewGuid():N}}"}""",
                constructor.GetParameters()[1].ParameterType);
            Project = (Project)constructor.Invoke(new[] { Folder, dto });
            Settings.InitContext<TestContext>(Project);
        }

        public async Task SaveUser()
        {
            var contexts = (Dictionary<SettingLevel, List<SettingContext>>)Settings.GetType()
                .GetProperty("Contexts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Settings);
            var save = new SettingSaveContext();
            save.Init(Resources);
            save.Settings.Add(contexts[SettingLevel.User].Single());
            await save.SaveAction(Project);
        }

        public void Dispose()
        {
            var userFolder = Settings.GetPath(Project, SettingLevel.User);
            if (Directory.Exists(userFolder)) Directory.Delete(userFolder, true);
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        }
    }
}
