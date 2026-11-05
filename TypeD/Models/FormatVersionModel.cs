using System.Text.Json;
using System.Text.Json.Nodes;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal sealed class FormatVersionModel : IFormatVersionModel
    {
        private const string MetadataName = "FormatVersions";
        private readonly Dictionary<(FormatDocumentKind Kind, string Module), FormatVersionDefinition> Definitions = new();
        private readonly object SyncRoot = new();
        private ILogModel LogModel;

        public FormatVersionModel()
        {
            foreach (var kind in Enum.GetValues<FormatDocumentKind>())
                Register("TypeD", new FormatVersionDefinition(kind, 1,
                    new Dictionary<int, Action<JsonObject>> { [0] = _ => { } }));
        }

        public void Init(IResourceModel resourceModel)
        {
            LogModel = resourceModel.Get<ILogModel>();
        }

        public IDisposable Register(string moduleId, FormatVersionDefinition definition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
            ArgumentNullException.ThrowIfNull(definition);
            if (!Enum.IsDefined(definition.Kind) || definition.CurrentVersion < 1)
                throw new ArgumentException("A format definition needs a known document kind and a positive version.", nameof(definition));
            ArgumentNullException.ThrowIfNull(definition.Conversions);
            var steps = new Dictionary<int, Action<JsonObject>>(definition.Conversions);
            if (steps.Any(step => step.Key < 0 || step.Key >= definition.CurrentVersion || step.Value == null))
                throw new ArgumentException("Conversion source versions must be non-negative and below the current version.", nameof(definition));
            var key = (definition.Kind, moduleId);
            var snapshot = new FormatVersionDefinition(definition.Kind, definition.CurrentVersion, steps);
            lock (SyncRoot)
            {
                if (!Definitions.TryAdd(key, snapshot))
                    throw new InvalidOperationException($"Format versions for '{moduleId}' ({definition.Kind}) are already registered.");
            }
            return new Registration(() =>
            {
                lock (SyncRoot)
                    Definitions.Remove(key);
            });
        }

        public Dictionary<string, int> GetCurrentVersions(FormatDocumentKind kind, IReadOnlyDictionary<string, int> stored = null)
        {
            if (!Enum.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            var result = stored == null ? new Dictionary<string, int>(StringComparer.Ordinal)
                : new Dictionary<string, int>(stored, StringComparer.Ordinal);
            ValidateVersions(result, kind, false);
            foreach (var entry in ForKind(kind))
            {
                if (stored?.Count > 0 && result.GetValueOrDefault(entry.Key.Module) < entry.Value.CurrentVersion)
                    throw new InvalidDataException(
                        $"The {kind} document needs conversion for '{entry.Key.Module}'. Reopen the project before saving with the new format.");
                result[entry.Key.Module] = entry.Value.CurrentVersion;
            }
            return result;
        }

        public JsonObject Upgrade(JsonObject document, FormatDocumentKind kind, bool coreOnly = false)
        {
            ArgumentNullException.ThrowIfNull(document);
            if (!Enum.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            var result = (JsonObject)document.DeepClone();
            var versions = ReadVersions(result);
            ValidateVersions(versions, kind, coreOnly);
            foreach (var entry in ForKind(kind).Where(entry => !coreOnly || entry.Key.Module == "TypeD"))
            {
                var version = versions.GetValueOrDefault(entry.Key.Module);
                while (version < entry.Value.CurrentVersion)
                {
                    if (!entry.Value.Conversions.TryGetValue(version, out var convert))
                        throw new InvalidDataException($"Missing {kind} conversion for '{entry.Key.Module}' from version {version} to {version + 1}.");
                    try
                    {
                        convert(result);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidDataException(
                            $"Failed {kind} conversion for '{entry.Key.Module}' from version {version} to {version + 1}.", exception);
                    }
                    version++;
                    versions[entry.Key.Module] = version;
                }
            }
            // Converters own format data, not the version bookkeeping.
            result[MetadataName] = JsonSerializer.SerializeToNode(versions);
            return result;
        }

        public T Read<T>(string path, FormatDocumentKind kind, bool coreOnly = false)
        {
            var original = File.ReadAllText(path);
            try
            {
                var document = Parse(original);
                var upgraded = Upgrade(document, kind, coreOnly);
                var value = upgraded.Deserialize<T>(JSON.CreateOptions())
                    ?? throw new InvalidDataException("The document deserialized to null.");
                if (!coreOnly && !JsonNode.DeepEquals(document, upgraded))
                    Commit(new[] { new PendingUpgrade(path, original, upgraded.ToJsonString(JSON.CreateOptions())) });
                return value;
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                throw new InvalidDataException($"Could not load {kind} file '{path}': {exception.Message}", exception);
            }
        }

        public bool UpgradeFiles(IEnumerable<(string Path, FormatDocumentKind Kind)> files,
            Action<string, JsonObject> validate = null)
        {
            var pending = new List<PendingUpgrade>();
            foreach (var file in files.DistinctBy(file => Path.GetFullPath(file.Path), StringComparer.OrdinalIgnoreCase))
            {
                var original = File.ReadAllText(file.Path);
                try
                {
                    var document = Parse(original);
                    var upgraded = Upgrade(document, file.Kind);
                    if (file.Kind == FormatDocumentKind.Project)
                        upgraded.Deserialize<ProjectDTO>(JSON.CreateOptions());
                    else
                        upgraded.Deserialize<ComponentDTO>(JSON.CreateOptions());
                    validate?.Invoke(file.Path, upgraded);
                    if (!JsonNode.DeepEquals(document, upgraded))
                        pending.Add(new PendingUpgrade(file.Path, original, upgraded.ToJsonString(JSON.CreateOptions())));
                }
                catch (Exception exception) when (exception is JsonException or InvalidDataException)
                {
                    throw new InvalidDataException($"Could not upgrade {file.Kind} file '{file.Path}': {exception.Message}", exception);
                }
            }
            Commit(pending);
            return pending.Count > 0;
        }

        private IEnumerable<KeyValuePair<(FormatDocumentKind Kind, string Module), FormatVersionDefinition>> ForKind(FormatDocumentKind kind)
        {
            lock (SyncRoot)
            {
                return Definitions.Where(entry => entry.Key.Kind == kind)
                    .OrderBy(entry => entry.Key.Module == "TypeD" ? 0 : 1)
                    .ThenBy(entry => entry.Key.Module, StringComparer.Ordinal).ToList();
            }
        }

        private void ValidateVersions(IReadOnlyDictionary<string, int> versions, FormatDocumentKind kind, bool coreOnly)
        {
            foreach (var entry in versions)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value < 0)
                    throw new InvalidDataException("Format versions must have non-empty module ids and non-negative integer values.");
                if (coreOnly && entry.Key != "TypeD")
                    continue;
                FormatVersionDefinition definition;
                lock (SyncRoot)
                    Definitions.TryGetValue((kind, entry.Key), out definition);
                if (definition == null)
                    throw new InvalidDataException($"No format handler is loaded for '{entry.Key}' ({kind}, version {entry.Value}).");
                if (entry.Value > definition.CurrentVersion)
                    throw new InvalidDataException(
                        $"Unsupported future {kind} format for '{entry.Key}': file version {entry.Value}, supported version {definition.CurrentVersion}.");
            }
        }

        private static JsonObject Parse(string json)
        {
            return JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Expected a JSON object.");
        }

        private static Dictionary<string, int> ReadVersions(JsonObject document)
        {
            if (!document.TryGetPropertyValue(MetadataName, out var metadata))
                return new Dictionary<string, int>(StringComparer.Ordinal);
            if (metadata is not JsonObject entries)
                throw new InvalidDataException("FormatVersions must be an object of module ids and integer versions.");
            var versions = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry.Value is not JsonValue value || !value.TryGetValue<int>(out var version))
                    throw new InvalidDataException($"Format version for '{entry.Key}' must be an integer.");
                versions.Add(entry.Key, version);
            }
            return versions;
        }

        private void Commit(IEnumerable<PendingUpgrade> upgrades)
        {
            var staged = new List<(PendingUpgrade Upgrade, string Temporary, string Backup)>();
            try
            {
                foreach (var upgrade in upgrades)
                {
                    if (File.ReadAllText(upgrade.Path) != upgrade.Original)
                        throw new IOException($"'{upgrade.Path}' changed during format conversion; refusing to overwrite it.");
                    var temporary = upgrade.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    var backup = upgrade.Path + "." + Guid.NewGuid().ToString("N") + ".bak";
                    staged.Add((upgrade, temporary, backup));
                    File.WriteAllText(temporary, upgrade.Converted);
                }
                foreach (var file in staged)
                {
                    if (File.ReadAllText(file.Upgrade.Path) != file.Upgrade.Original)
                        throw new IOException($"'{file.Upgrade.Path}' changed during format conversion; refusing to overwrite it.");
                    File.Replace(file.Temporary, file.Upgrade.Path, file.Backup);
                    LogModel?.Log($"Upgraded '{file.Upgrade.Path}'. Original saved to '{file.Backup}'.");
                }
            }
            finally
            {
                foreach (var file in staged)
                    if (File.Exists(file.Temporary))
                        File.Delete(file.Temporary);
            }
        }

        private sealed record PendingUpgrade(string Path, string Original, string Converted);

        private sealed class Registration(Action remove) : IDisposable
        {
            private Action Remove = remove;
            public void Dispose() => Interlocked.Exchange(ref Remove, null)?.Invoke();
        }
    }
}
