namespace TypeD.Models.Data
{
    /// <summary>
    /// A single warning or error reported by a build.
    /// </summary>
    public class BuildIssue
    {
        /// <summary>Gets or sets the severity, e.g. "warning" or "error".</summary>
        public string Severity { get; set; }
        /// <summary>Gets or sets the compiler diagnostic code, e.g. "CS1591".</summary>
        public string Code { get; set; }
        /// <summary>Gets or sets the diagnostic message text.</summary>
        public string Message { get; set; }
        /// <summary>Gets or sets the path of the file the issue was reported in.</summary>
        public string File { get; set; }
        /// <summary>Gets or sets the line number of the issue, if known.</summary>
        public int? Line { get; set; }
        /// <summary>Gets or sets the column number of the issue, if known.</summary>
        public int? Column { get; set; }
        /// <summary>Gets or sets the name of the game project or module the issue comes from.</summary>
        public string Source { get; set; }
        /// <summary>Gets or sets a value indicating whether the issue comes from a module instead of the game project.</summary>
        public bool IsModuleIssue { get; set; }
        /// <summary>Gets or sets the path of the C# project that reported the issue, if known.</summary>
        public string ProjectFile { get; set; }

        /// <summary>
        /// Formats the issue like a compiler message: source, location, severity, code and message.
        /// </summary>
        /// <returns>The formatted issue.</returns>
        public override string ToString()
        {
            var source = string.IsNullOrEmpty(Source) ? string.Empty : $"[{(IsModuleIssue ? "Module" : "Game")} {Source}] ";
            var location = string.IsNullOrEmpty(File)
                ? string.Empty
                : $"{File}{(Line.HasValue ? $"({Line}{(Column.HasValue ? $",{Column}" : string.Empty)})" : string.Empty)}: ";
            var code = string.IsNullOrEmpty(Code) ? string.Empty : $"{Code}: ";
            return $"{source}{location}{Severity} {code}{Message}";
        }
    }
}
