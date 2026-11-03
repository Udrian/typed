namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired whenever the build state of a project changes (build started, progress, finished).
    /// </summary>
    public class BuildStatusChangedHook : Hook
    {
        /// <summary>Gets or sets the project being built.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets a value indicating whether a build is currently running.</summary>
        public bool IsBuilding { get; set; }
        /// <summary>Gets or sets a value indicating whether the build has finished.</summary>
        public bool IsComplete { get; set; }
        /// <summary>Gets or sets a value indicating whether the finished build succeeded.</summary>
        public bool Succeeded { get; set; }
        /// <summary>Gets or sets the status text describing the current build state.</summary>
        public string Status { get; set; }
        /// <summary>Gets or sets the warnings reported by the build.</summary>
        public List<BuildIssue> Warnings { get; set; } = new();
        /// <summary>Gets or sets the errors reported by the build.</summary>
        public List<BuildIssue> Errors { get; set; } = new();
    }
}
