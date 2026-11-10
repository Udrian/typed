namespace TypeD.Models.Data
{
    /// <summary>Severity of a game log entry, mirrors the engine's log levels.</summary>
    public enum GameLogLevel
    {
        /// <summary>No severity.</summary>
        None = 0,
        /// <summary>Detailed diagnostic output.</summary>
        Ludacris = 1,
        /// <summary>Debugging information.</summary>
        Debug = 2,
        /// <summary>General informational messages.</summary>
        Info = 3,
        /// <summary>Potential problems that do not stop execution.</summary>
        Warning = 4,
        /// <summary>Errors that affect an operation.</summary>
        Error = 5,
        /// <summary>A fatal error that may prevent continued execution.</summary>
        Fatal = 6
    }

    /// <summary>A single log line produced by the game running inside the editor.</summary>
    public class GameLogEntry
    {
        /// <summary>Gets the time the entry was logged.</summary>
        public DateTime Time { get; init; }
        /// <summary>Gets the severity of the entry.</summary>
        public GameLogLevel Level { get; init; }
        /// <summary>Gets the logged message.</summary>
        public string Message { get; init; }

        /// <summary>Gets the four letter tag of the severity, matching the engine's default logger.</summary>
        public string LevelTag => Level switch
        {
            GameLogLevel.Ludacris => "LDCR",
            GameLogLevel.Debug => "DBUG",
            GameLogLevel.Info => "INFO",
            GameLogLevel.Warning => "WARN",
            GameLogLevel.Error => "EROR",
            GameLogLevel.Fatal => "FATL",
            _ => "NONE"
        };

        /// <summary>Formats the entry as a single log line.</summary>
        /// <returns>The formatted line.</returns>
        public string Format()
        {
            return $"[{Time:HH:mm:ss}] {LevelTag}: {Message}";
        }
    }
}
