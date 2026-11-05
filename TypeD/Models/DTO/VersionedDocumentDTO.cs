using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypeD.Models.DTO
{
    /// <summary>Version metadata and module-owned fields shared by persisted editor documents.</summary>
    public abstract class VersionedDocumentDTO
    {
        /// <summary>Gets or sets the independent format version of each participating module.</summary>
        public Dictionary<string, int> FormatVersions { get; set; } = new();

        /// <summary>Gets or sets additional module-owned JSON fields preserved across saves.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; } = new();
    }
}
