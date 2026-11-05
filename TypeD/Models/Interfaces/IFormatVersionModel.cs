using System.Text.Json.Nodes;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Registers format versions and safely upgrades project and component JSON.</summary>
    public interface IFormatVersionModel : IModel
    {
        /// <summary>Registers a module's format definition until the returned registration is disposed.</summary>
        IDisposable Register(string moduleId, FormatVersionDefinition definition);

        /// <summary>Gets versions for new files or already converted documents; rejects unsupported or unconverted stored versions.</summary>
        Dictionary<string, int> GetCurrentVersions(FormatDocumentKind kind, IReadOnlyDictionary<string, int> stored = null);

        /// <summary>Upgrades a copy of the JSON. Core-only mode is used to bootstrap project module loading.</summary>
        JsonObject Upgrade(JsonObject document, FormatDocumentKind kind, bool coreOnly = false);

        /// <summary>Reads and validates a document, backing up and saving upgrades unless core-only mode is requested.</summary>
        T Read<T>(string path, FormatDocumentKind kind, bool coreOnly = false);

        /// <summary>Validates every upgrade before backing up and replacing any of the supplied files.</summary>
        /// <returns>Whether any file was upgraded.</returns>
        bool UpgradeFiles(IEnumerable<(string Path, FormatDocumentKind Kind)> files,
            Action<string, JsonObject> validate = null);
    }
}
