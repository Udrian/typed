namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the game hosted inside the editor has been reloaded and can start again.
    /// Listeners that shut down their game instances on <see cref="GameStopHook"/> should recreate them.
    /// </summary>
    public class GameStartHook : Hook
    {
        /// <summary>Gets or sets the project whose game is started.</summary>
        public Project Project { get; set; }

        /// <summary>Initializes a new instance of the <see cref="GameStartHook"/> class.</summary>
        public GameStartHook() { }
    }
}
