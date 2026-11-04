using System.Text.Json;
using TypeD.Helpers;

namespace TypeD.Models.Data.SettingContexts
{
    /// <summary>Workspace branding paths, relative to the project folder. Empty paths use the editor defaults.</summary>
    [Name("Branding")]
    public class BrandingSettingContext : SettingContext
    {
        /// <summary>Gets or sets the project window icon path.</summary>
        public Setting<string> Icon { get; set; } = new(string.Empty);
        /// <summary>Gets or sets the project splash image path.</summary>
        public Setting<string> Splash { get; set; } = new(string.Empty);

        /// <summary>Creates branding settings saved with the workspace.</summary>
        public BrandingSettingContext()
        {
            SaveOnExit = true;
        }

        /// <summary>Reads workspace branding before the project opens, converting the legacy flat settings when needed.</summary>
        public static BrandingSettingContext LoadWorkspace(string projectLocation)
        {
            var context = new BrandingSettingContext();
            context.Init(SettingLevel.Project);
            if (string.IsNullOrEmpty(projectLocation))
                return context;

            var path = Path.Combine(projectLocation, "TypeO", "Settings", "Branding.json");
            if (!File.Exists(path))
                return context;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var legacy = false;
            context.Icon.Value = ReadPath(document.RootElement, nameof(Icon), ref legacy);
            context.Splash.Value = ReadPath(document.RootElement, nameof(Splash), ref legacy);
            if (legacy)
                JSON.Serialize(context, path);
            return context;
        }

        private static string ReadPath(JsonElement root, string name, ref bool legacy)
        {
            if (!root.TryGetProperty(name, out var property))
                return string.Empty;
            if (property.ValueKind is JsonValueKind.String or JsonValueKind.Null)
            {
                legacy = true;
                return property.GetString() ?? string.Empty;
            }
            return property.GetProperty(nameof(Setting<string>.Value)).GetString() ?? string.Empty;
        }
    }
}
