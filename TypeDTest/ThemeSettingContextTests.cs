using System.Reflection;
using System.Text.Json;
using TypeD;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Data.SettingContexts;
using TypeD.Models.Interfaces;

namespace TypeDTest;

public class ThemeSettingContextTests
{
    [Theory]
    [InlineData("{\"Mode\":\"Dark\",\"Scheme\":\"Paper Plane\"}", "Dark", "Paper Plane")]
    [InlineData("{\"Mode\":{\"Value\":\"System\"},\"Scheme\":{\"Value\":\"Grape Soda\"}}", "System", "Grape Soda")]
    [InlineData("{\"Mode\":null,\"Scheme\":null}", "", "")]
    public void ReadsLegacyAndContextFormats(string json, string mode, string scheme)
    {
        var context = JsonSerializer.Deserialize<ThemeSettingContext>(json);

        Assert.Equal(mode, context.Mode.Value);
        Assert.Equal(scheme, context.Scheme.Value);
        Assert.Empty(context.Mode.DefaultValue);
        var roundTrip = JsonSerializer.Deserialize<ThemeSettingContext>(JsonSerializer.Serialize(context));
        Assert.Equal(mode, roundTrip.Mode.Value);
        Assert.Equal(scheme, roundTrip.Scheme.Value);
    }

    [Fact]
    public void UserOverridesProjectAndSystemAndCanRestoreInheritance()
    {
        var (_, settings) = Initialize();
        settings.SetContext(Choice("Dark", "Paper Plane"), SettingLevel.System);
        settings.SetContext(Choice("Light", "Mint Condition"), SettingLevel.Project);
        settings.SetContext(Choice("System", "Grape Soda"), SettingLevel.User);

        var effective = settings.GetContext<TestThemeContext>();
        Assert.Equal("System", effective.Mode.Value);
        Assert.Equal("Grape Soda", effective.Scheme.Value);
        Assert.Equal(SettingLevel.User, effective.Mode.Level);

        settings.SetContext(new TestThemeContext(), SettingLevel.User);
        effective = settings.GetContext<TestThemeContext>();
        Assert.Equal("Light", effective.Mode.Value);
        Assert.Equal("Mint Condition", effective.Scheme.Value);
        Assert.Equal(SettingLevel.Project, effective.Scheme.Level);

        settings.SetContext(new TestThemeContext(), SettingLevel.Project);
        effective = settings.GetContext<TestThemeContext>();
        Assert.Equal("Dark", effective.Mode.Value);
        Assert.Equal("Paper Plane", effective.Scheme.Value);
        Assert.Equal(SettingLevel.System, effective.Mode.Level);
    }

    [Fact]
    public void MissingValuesInheritWithoutFreezingLowerLevelChoices()
    {
        var (_, settings) = Initialize();
        settings.SetContext(Choice("Dark", "Paper Plane"), SettingLevel.System);
        settings.SetContext(Choice("", "Mint Condition"), SettingLevel.Project);
        settings.SetContext(Choice("Light", ""), SettingLevel.User);

        Assert.Equal("Light", settings.GetContext<TestThemeContext>().Mode.Value);
        Assert.Equal("Mint Condition", settings.GetContext<TestThemeContext>().Scheme.Value);
        settings.SetContext(Choice("", "Cozy Cabin"), SettingLevel.Project);
        Assert.Equal("Cozy Cabin", settings.GetContext<TestThemeContext>().Scheme.Value);
    }

    [Fact]
    public async Task ProjectSettingsPersistAndReloadInContextFormat()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ThemeTest-{Guid.NewGuid():N}");
        string userDirectory = null;
        Directory.CreateDirectory(directory);
        try
        {
            var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var data = Activator.CreateInstance(constructor.GetParameters()[1].ParameterType);
            data.GetType().GetProperty("ProjectName").SetValue(data, $"ThemeTest-{Guid.NewGuid():N}");
            var project = (Project)constructor.Invoke(new[] { directory, data });
            var (resources, settings) = Initialize();
            var context = Choice("Dark", "Paper Plane");
            settings.SetContext(context, SettingLevel.Project);
            var save = new SettingSaveContext();
            save.Init(resources);
            save.Settings.Add(context);

            await save.SaveAction(project);

            var path = Path.Combine(settings.GetPath(project, SettingLevel.Project), settings.GetName<TestThemeContext>() + ".json");
            var saved = JSON.Deserialize<ThemeSettingContext>(path);
            Assert.Equal("Dark", saved.Mode.Value);
            Assert.Equal("Paper Plane", saved.Scheme.Value);
            Assert.False(context.ShouldSave);
            settings.InitContext<TestThemeContext>(project);
            Assert.Equal("Dark", settings.GetContext<TestThemeContext>().Mode.Value);
            Assert.Equal(SettingLevel.Project, settings.GetContext<TestThemeContext>().Mode.Level);

            var userPath = settings.GetPath(project, SettingLevel.User);
            Assert.False(Directory.Exists(userPath));
            userDirectory = userPath;
            var user = Choice("Light", "Mint Condition");
            settings.SetContext(user, SettingLevel.User);
            save.Settings.Clear();
            save.Settings.Add(user);
            await save.SaveAction(project);
            settings.InitContext<TestThemeContext>(project);
            Assert.Equal("Light", settings.GetContext<TestThemeContext>().Mode.Value);
            Assert.Equal(SettingLevel.User, settings.GetContext<TestThemeContext>().Mode.Level);

            user = new TestThemeContext();
            settings.SetContext(user, SettingLevel.User);
            save.Settings.Clear();
            save.Settings.Add(user);
            await save.SaveAction(project);
            settings.InitContext<TestThemeContext>(project);
            Assert.Equal("Dark", settings.GetContext<TestThemeContext>().Mode.Value);
            Assert.Equal("Paper Plane", settings.GetContext<TestThemeContext>().Scheme.Value);
            Assert.Equal(SettingLevel.Project, settings.GetContext<TestThemeContext>().Mode.Level);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
            if (userDirectory != null && Directory.Exists(userDirectory))
                Directory.Delete(userDirectory, recursive: true);
        }
    }

    private static TestThemeContext Choice(string mode, string scheme) => new()
    {
        Mode = new Setting<string>(string.Empty) { Value = mode },
        Scheme = new Setting<string>(string.Empty) { Value = scheme }
    };

    private static (TestResourceModel Resources, ISettingModel Settings) Initialize()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var settings = resources.Get<ISettingModel>();
        settings.InitContext<TestThemeContext>(null);
        return (resources, settings);
    }

    [Name("ThemeSettingContextTests-737928e6")]
    public class TestThemeContext : ThemeSettingContext { }
}
