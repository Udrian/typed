using System.Text.Json;
using TypeD.Models.Data;
using TypeD.Models.Data.SettingContexts;

namespace TypeDTest;

public class BrandingSettingContextTests
{
    [Theory]
    [InlineData("{\"Icon\":\"TypeO/Branding/Icon.png\",\"Splash\":null}", "TypeO/Branding/Icon.png", "")]
    [InlineData("{\"Icon\":{\"Value\":\"icon.png\"},\"Splash\":{\"Value\":\"splash.png\"}}", "icon.png", "splash.png")]
    public void LoadWorkspace_ReadsSettingsAndMigratesLegacyPaths(string json, string icon, string splash)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"BrandingTest-{Guid.NewGuid():N}");
        var settings = Path.Combine(directory, "TypeO", "Settings");
        Directory.CreateDirectory(settings);
        var path = Path.Combine(settings, "Branding.json");
        File.WriteAllText(path, json);
        try
        {
            var context = BrandingSettingContext.LoadWorkspace(directory);

            Assert.Equal(SettingLevel.Project, context.Level);
            Assert.Equal(SettingLevel.Project, context.Icon.Level);
            Assert.Equal(icon, context.Icon.Value);
            Assert.Equal(splash, context.Splash.Value);
            using var saved = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(icon, saved.RootElement.GetProperty("Icon").GetProperty("Value").GetString());
            var reloaded = BrandingSettingContext.LoadWorkspace(directory);
            Assert.Equal(icon, reloaded.Icon.Value);
            Assert.Equal(splash, reloaded.Splash.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadWorkspace_WithoutProjectUsesDefaults()
    {
        var context = BrandingSettingContext.LoadWorkspace(null);

        Assert.Equal(SettingLevel.Project, context.Level);
        Assert.Empty(context.Icon.Value);
        Assert.Empty(context.Splash.Value);
    }
}
