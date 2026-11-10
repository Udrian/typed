using System.Text;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class GameLogModel : IGameLogModel
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
        public event Action Cleared;
        public event Action LevelColorsChanged;

        public void Init(IResourceModel resourceModel)
        {
        }

        // Functions
        public void Add(GameLogLevel level, string message)
        {
            var entry = new GameLogEntry()
            {
                Time = DateTime.Now,
                Level = level,
                Message = message ?? string.Empty
            };
            lock (EntriesLock)
            {
                Entries.Add(entry);
                if (Entries.Count > MaxEntries)
                    Entries.RemoveRange(0, Entries.Count - MaxEntries);
            }
            EntryAdded?.Invoke(entry);
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
