using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TypeD.Models.Data
{
    /// <summary>Persists the last successful project save time for autosave recovery checks.</summary>
    public static class ProjectSaveState
    {
        private sealed class State
        {
            public DateTime LastSavedUtc { get; set; }
            public DateTime LatestActivityUtc { get; set; }
            public bool LatestActivityWasAutosave { get; set; }
        }

        /// <summary>Describes the latest save or autosave activity for a project.</summary>
        public sealed record SaveActivity(DateTime TimestampUtc, bool IsAutosave);

        private static string GetStatePath(Project project)
        {
            var projectPath = Path.GetFullPath(project.ProjectFilePath);
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(projectPath)));
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Typedeaf", "TypeO", "SaveState", $"{key}.txt");
        }

        /// <summary>Records the current time as the project's latest successful save.</summary>
        /// <param name="project">The project that was saved.</param>
        public static void Record(Project project)
        {
            var now = DateTime.UtcNow;
            WriteState(project, new State
            {
                LastSavedUtc = now,
                LatestActivityUtc = now,
                LatestActivityWasAutosave = false
            });
        }

        /// <summary>Records the time of a completed autosave without changing the last regular save time.</summary>
        /// <param name="project">The project that was autosaved.</param>
        public static void RecordAutosave(Project project)
        {
            var current = ReadState(project);
            current.LatestActivityUtc = DateTime.UtcNow;
            current.LatestActivityWasAutosave = true;
            WriteState(project, current);
        }

        /// <summary>Gets the latest save time, falling back to the project file timestamp.</summary>
        /// <param name="project">The project to check.</param>
        /// <returns>The latest known save time in UTC.</returns>
        public static DateTime GetLastSavedUtc(Project project)
        {
            var state = ReadState(project);
            if (state.LastSavedUtc != DateTime.MinValue)
                return state.LastSavedUtc;

            return File.Exists(project.ProjectFilePath)
                ? File.GetLastWriteTimeUtc(project.ProjectFilePath)
                : DateTime.MinValue;
        }

        /// <summary>Gets the latest save activity, including whether it was an autosave.</summary>
        /// <param name="project">The project to check.</param>
        /// <returns>The latest save activity.</returns>
        public static SaveActivity GetLatestActivity(Project project)
        {
            var state = ReadState(project);
            if (state.LatestActivityUtc != DateTime.MinValue)
                return new SaveActivity(state.LatestActivityUtc, state.LatestActivityWasAutosave);

            var lastSaved = GetLastSavedUtc(project);
            return new SaveActivity(lastSaved, false);
        }

        private static State ReadState(Project project)
        {
            var path = GetStatePath(project);
            if (!File.Exists(path))
                return new State();

            var contents = File.ReadAllText(path);
            try
            {
                return JsonSerializer.Deserialize<State>(contents) ?? new State();
            }
            catch (JsonException)
            {
                if (DateTime.TryParse(contents, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var legacyTimestamp))
                {
                    var timestamp = legacyTimestamp.ToUniversalTime();
                    return new State { LastSavedUtc = timestamp, LatestActivityUtc = timestamp };
                }

                throw;
            }
        }

        private static void WriteState(Project project, State state)
        {
            var path = GetStatePath(project);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonSerializer.Serialize(state));
        }
    }
}
