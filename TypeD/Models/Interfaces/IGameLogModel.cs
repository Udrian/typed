using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that keeps the log of the game running inside the editor.</summary>
    public interface IGameLogModel : IModel
    {
        /// <summary>Raised when an entry is added to the log.</summary>
        public event Action<GameLogEntry> EntryAdded;
        /// <summary>Raised when the log is cleared.</summary>
        public event Action Cleared;
        /// <summary>Raised when the color of one or more log levels changed.</summary>
        public event Action LevelColorsChanged;

        /// <summary>Adds an entry to the log.</summary>
        /// <param name="level">The severity of the entry.</param>
        /// <param name="message">The message to log.</param>
        public void Add(GameLogLevel level, string message);
        /// <summary>Clears the log.</summary>
        public void Clear();
        /// <summary>Gets a snapshot of the logged entries.</summary>
        /// <returns>The entries, oldest first.</returns>
        public List<GameLogEntry> GetEntries();
        /// <summary>Gets the whole log as formatted text, for example for crash reports.</summary>
        /// <returns>The formatted log.</returns>
        public string GetText();
        /// <summary>Gets the display color of a log level as a hex string, for example "#FFC02E".</summary>
        /// <param name="level">The log level.</param>
        /// <returns>The hex color.</returns>
        public string GetLevelColor(GameLogLevel level);
        /// <summary>Sets the display color of a log level.</summary>
        /// <param name="level">The log level.</param>
        /// <param name="hexColor">The hex color, for example "#FFC02E".</param>
        public void SetLevelColor(GameLogLevel level, string hexColor);
    }
}
