using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypeD.Models.Data.SettingContexts
{
    /// <summary>Theme choices. Empty values inherit the choice from the lower setting level.</summary>
    [Name("Theme")]
    public class ThemeSettingContext : SettingContext
    {
        /// <summary>Gets or sets the mode: System, Light, or Dark.</summary>
        [JsonConverter(typeof(ThemeValueConverter))]
        public Setting<string> Mode { get; set; } = new(string.Empty);

        /// <summary>Gets or sets the color scheme name.</summary>
        [JsonConverter(typeof(ThemeValueConverter))]
        public Setting<string> Scheme { get; set; } = new(string.Empty);

        /// <summary>Creates theme settings saved on editor exit.</summary>
        public ThemeSettingContext() { SaveOnExit = true; }
    }

    /// <summary>Reads both legacy flat theme values and setting values.</summary>
    public sealed class ThemeValueConverter : JsonConverter<Setting<string>>
    {
        /// <inheritdoc/>
        public override bool HandleNull => true;

        /// <inheritdoc/>
        public override Setting<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var value = document.RootElement;
            if (value.ValueKind == JsonValueKind.Object)
            {
                if (!value.TryGetProperty("Value", out value))
                    throw new JsonException("Theme setting is missing Value.");
            }
            if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                throw new JsonException("Theme setting must contain a string.");
            return new Setting<string>(string.Empty) { Value = value.GetString() ?? string.Empty };
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Setting<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Value", value.Value);
            writer.WriteEndObject();
        }
    }
}
