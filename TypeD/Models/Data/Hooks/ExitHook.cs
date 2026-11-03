namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the application is exiting, so listeners can clean up.
    /// </summary>
    public class ExitHook : Hook
    {
        /// <summary>Gets or sets the project that was open when the application exits.</summary>
        public Project Project { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExitHook"/> class.</summary>
        public ExitHook() { }
    }
}
