using System.Text;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class GameLogModel : IBatchedGameLogModel
    {
        private const int MaxEntries = 5000;
        private const string FallbackColor = "#D4D4D4";

        // Data
        private readonly object EntriesLock = new();
        private List<GameLogEntry> Entries { get; } = new();
        private Dictionary<GameLogLevel, string> LevelColors { get; } = new()
        {
            { GameLogLevel.Ludacris, "#9E9E9E" },
            { GameLogLevel.Debug,    "#4EC9B0" },
            { GameLogLevel.Info,     "#D4D4D4" },
            { GameLogLevel.Warning,  "#FFC02E" },
            { GameLogLevel.Error,    "#F85149" },
            { GameLogLevel.Fatal,    "#FF00FF" }
        };

        // Events
        public event Action<GameLogEntry> EntryAdded;
        public event Action<IReadOnlyList<GameLogEntry>, bool> EntriesAdded;
        public event Action Cleared;
        public event Action LevelColorsChanged;

        public void Init(IResourceModel resourceModel)
        {
        }

        // Functions
        public void Add(GameLogLevel level, string message)
        {
            AddRange(new[]
            {
                new GameLogEntry()
                {
                    Time = DateTime.Now,
                    Level = level,
                    Message = message ?? string.Empty
                }
            });
        }

        public void AddRange(IReadOnlyList<GameLogEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            if (entries.Count == 0)
                return;

            var addedEntries = new GameLogEntry[entries.Count];
            for (var i = 0; i < entries.Count; i++)
                addedEntries[i] = entries[i] ?? throw new ArgumentException("Log entries cannot contain null values.", nameof(entries));

            bool trimmed;
            lock (EntriesLock)
            {
                Entries.AddRange(addedEntries);
                trimmed = Entries.Count > MaxEntries;
                if (trimmed)
                    Entries.RemoveRange(0, Entries.Count - MaxEntries);
            }

            var entryAdded = EntryAdded;
            if (entryAdded != null)
            {
                foreach (var entry in addedEntries)
                    entryAdded.Invoke(entry);
            }
            EntriesAdded?.Invoke(addedEntries, trimmed);
        }

        public void Clear()
        {
            lock (EntriesLock)
            {
                Entries.Clear();
            }
            Cleared?.Invoke();
        }

        public List<GameLogEntry> GetEntries()
        {
            lock (EntriesLock)
            {
                return new List<GameLogEntry>(Entries);
            }
        }

        public string GetText()
        {
            var builder = new StringBuilder();
            foreach (var entry in GetEntries())
            {
                builder.AppendLine(entry.Format());
            }
            return builder.ToString();
        }

        public string GetLevelColor(GameLogLevel level)
        {
            lock (EntriesLock)
            {
                return LevelColors.TryGetValue(level, out var color) ? color : FallbackColor;
            }
        }

        public void SetLevelColor(GameLogLevel level, string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;
            lock (EntriesLock)
            {
                if (LevelColors.TryGetValue(level, out var current) && current == hexColor) return;
                LevelColors[level] = hexColor;
            }
            LevelColorsChanged?.Invoke();
        }
    }
}
