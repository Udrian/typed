namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the game hosted inside the editor must stop, for example after it crashed or before it is reloaded.
    /// Listeners running the game, such as viewers, should shut down their game instances.
    /// </summary>
    public class GameStopHook : Hook
    {
        /// <summary>Gets or sets the project whose game is stopped.</summary>
        public Project Project { get; set; }

        /// <summary>Initializes a new instance of the <see cref="GameStopHook"/> class.</summary>
        public GameStopHook() { }
    }
}
