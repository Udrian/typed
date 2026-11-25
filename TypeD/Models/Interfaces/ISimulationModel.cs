namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Controls how the update loop of the editor viewer simulates the game: playing or paused,
    /// a forced delta time and a speed multiplier.
    /// </summary>
    public interface ISimulationModel : IModel
    {
        /// <summary>The delta time, in seconds, that is forced when <see cref="ForceDeltaTime"/> is on.</summary>
        public const double DefaultForcedDeltaTime = 1.0 / 60.0;

        /// <summary>
        /// Gets or sets whether the update loop is running. When false the update loop is paused.
        /// </summary>
        bool IsPlaying { get; set; }
        /// <summary>
        /// Gets or sets whether <see cref="ForcedDeltaTime"/> replaces the real delta time.
        /// </summary>
        bool ForceDeltaTime { get; set; }
        /// <summary>
        /// Gets or sets the delta time, in seconds, used when <see cref="ForceDeltaTime"/> is on. Never negative.
        /// </summary>
        double ForcedDeltaTime { get; set; }
        /// <summary>
        /// Gets or sets the multiplier applied to the delta time. Never negative.
        /// </summary>
        double SpeedMultiplier { get; set; }

        /// <summary>
        /// Raised when any of the simulation settings change.
        /// </summary>
        event Action Changed;

        /// <summary>
        /// Raised when a reset of the simulation is requested, to reload and restart the current view.
        /// </summary>
        event Action ResetRequested;

        /// <summary>
        /// Requests the viewers to reload and restart the current view.
        /// </summary>
        void RequestReset();

        /// <summary>
        /// Gets the delta time to update the game with.
        /// </summary>
        /// <param name="deltaTime">The real delta time, in seconds, since the previous update.</param>
        /// <returns>The forced delta time if enabled, otherwise the real one, multiplied by the speed multiplier;
        /// or <c>null</c> when the simulation is paused.</returns>
        double? ResolveDeltaTime(double deltaTime);
    }
}
