using System.Text.Json.Nodes;

namespace TypeD.Models.Data
{
    /// <summary>The independently versioned JSON formats supported by the editor.</summary>
    public enum FormatDocumentKind
    {
        /// <summary>A .typeo project file.</summary>
        Project,
        /// <summary>A component definition JSON file.</summary>
        Component
    }

    /// <summary>A module's current format version and its consecutive upgrade steps.</summary>
    public sealed class FormatVersionDefinition
    {
        /// <summary>Gets the document format this definition handles.</summary>
        public FormatDocumentKind Kind { get; }
        /// <summary>Gets the current format version, independent of the module package version.</summary>
        public int CurrentVersion { get; }
        /// <summary>Gets the conversion from each old version to that version plus one.</summary>
        public IReadOnlyDictionary<int, Action<JsonObject>> Conversions { get; }

        /// <summary>Defines conversions of raw JSON before it is deserialized into editor objects.</summary>
        /// <param name="kind">The document format.</param>
        /// <param name="currentVersion">The version new and upgraded files will use.</param>
        /// <param name="conversions">Steps keyed by their source version; zero represents unversioned files.</param>
        public FormatVersionDefinition(FormatDocumentKind kind, int currentVersion,
            IReadOnlyDictionary<int, Action<JsonObject>> conversions)
        {
            Kind = kind;
            CurrentVersion = currentVersion;
            Conversions = conversions;
        }
    }
}
