using System;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Describes a crash that originated in the game project or in modules loaded inside the editor.
    /// </summary>
    public class CrashReport
    {
        /// <summary>Gets the project that was open when the crash happened.</summary>
        public Project Project { get; internal set; }
        /// <summary>Gets the exception that caused the crash.</summary>
        public Exception Exception { get; internal set; }
        /// <summary>Gets a value indicating whether the crash originated in the game project's code.</summary>
        public bool GameCrashed { get; internal set; }
        /// <summary>Gets the names of the modules the crash originated in.</summary>
        public List<string> CrashedModules { get; internal set; } = new List<string>();
        /// <summary>Gets the captured editor log output, including the game's console output, leading up to the crash.</summary>
        public string LogOutput { get; internal set; }
        /// <summary>Gets the log of the game running inside the editor, leading up to the crash.</summary>
        public string GameLog { get; internal set; }
    }
}
