using System;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Model that identifies crashes caused by the game project or the modules loaded inside the editor,
    /// captures the log output leading up to them, and reloads the game and modules within the editor.
    /// </summary>
    public interface ICrashModel : IModel
    {
        /// <summary>Gets or sets the project that is currently open in the editor.</summary>
        public Project CurrentProject { get; set; }
        /// <summary>Occurs when a crash of the game or a module is reported through <see cref="Report"/>.</summary>
        public event Action<CrashReport> Crashed;
        /// <summary>Identifies whether an exception originated in the game project or one of its loaded modules.</summary>
        /// <param name="exception">The unhandled exception.</param>
        /// <returns>A report describing the crash, or <c>null</c> when the exception did not come from the game or its modules.</returns>
        public CrashReport Identify(Exception exception);
        /// <summary>
        /// Identifies an exception and, when it came from the game or a module, stops the game and raises <see cref="Crashed"/>.
        /// Used by code hosting the game, such as viewers, that catches the game's exceptions itself.
        /// </summary>
        /// <param name="exception">The caught exception.</param>
        /// <returns><c>true</c> if the exception was a crash of the game or a module and has been reported.</returns>
        public bool Report(Exception exception);
        /// <summary>Stops the game hosted inside the editor by firing <see cref="Data.Hooks.GameStopHook"/>.</summary>
        /// <param name="project">The project whose game is stopped.</param>
        public void StopGame(Project project);
        /// <summary>
        /// Reloads the game inside the editor after a crash, without restarting the editor: the game is stopped, the crashed
        /// modules are unloaded and loaded again, the game project is rebuilt and reloaded, and the game is started again.
        /// </summary>
        /// <param name="report">The crash to recover from.</param>
        /// <param name="progress">Called with a description of each reload step.</param>
        /// <returns><c>true</c> if the game was reloaded and started again.</returns>
        public Task<bool> Reload(CrashReport report, Action<string> progress = null);
        /// <summary>Gets the recently captured log output of the editor and the game hosted inside it.</summary>
        /// <returns>The captured log lines.</returns>
        public string GetCapturedLog();
    }
}
