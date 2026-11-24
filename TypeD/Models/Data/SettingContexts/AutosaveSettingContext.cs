namespace TypeD.Models.Data.SettingContexts
{
    /// <summary>Autosave preferences for the editor.</summary>
    [Name("Autosave")]
    public sealed class AutosaveSettingContext : SettingContext
    {
        /// <summary>Gets or sets the minutes between autosaves; zero disables autosaving.</summary>
        public Setting<int> IntervalMinutes { get; set; } = new(5);
        /// <summary>Gets or sets the number of autosave snapshots to retain; zero disables autosave backups.</summary>
        public Setting<int> BackupCount { get; set; } = new(3);

        /// <summary>Initializes autosave preferences with autosaving enabled by default.</summary>
        public AutosaveSettingContext()
        {
            SaveOnExit = true;
        }
    }
}
