using TypeD.Commands;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Registry of editor commands that can be run from menus, the toolbar and keyboard shortcuts.</summary>
    public interface IEditorCommandRegistry : IModel
    {
        /// <summary>Gets all registered commands.</summary>
        IReadOnlyCollection<EditorCommand> Commands { get; }
        /// <summary>Registers a command that runs a delegate.</summary>
        /// <param name="id">The unique command id.</param>
        /// <param name="name">The display name.</param>
        /// <param name="description">The description of the command.</param>
        /// <param name="scope">The scope the command applies to.</param>
        /// <param name="defaultShortcut">The default shortcut, or <c>null</c> for none.</param>
        /// <param name="execute">The action to run.</param>
        /// <param name="canExecute">Optional condition that decides whether the command can run.</param>
        /// <param name="parameterProvider">Optional provider of the command parameter.</param>
        /// <returns>An object that unregisters the command when disposed.</returns>
        IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            Action<object> execute,
            Predicate<object> canExecute = null,
            Func<object> parameterProvider = null);
        /// <summary>Registers a command that is implemented by a custom command.</summary>
        /// <param name="id">The unique command id.</param>
        /// <param name="name">The display name.</param>
        /// <param name="description">The description of the command.</param>
        /// <param name="scope">The scope the command applies to.</param>
        /// <param name="defaultShortcut">The default shortcut, or <c>null</c> for none.</param>
        /// <param name="customCommand">The custom command to run.</param>
        /// <param name="parameterProvider">Optional provider of the command parameter.</param>
        /// <returns>An object that unregisters the command when disposed.</returns>
        IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            CustomCommand customCommand,
            Func<object> parameterProvider = null);
        /// <summary>Gets the custom command registered under an id.</summary>
        /// <typeparam name="T">The custom command type.</typeparam>
        /// <param name="id">The command id.</param>
        /// <returns>The custom command, or <c>null</c> if there is none.</returns>
        T GetCommand<T>(string id) where T : CustomCommand;
        /// <summary>Runs a command.</summary>
        /// <param name="id">The command id.</param>
        /// <param name="parameter">The command parameter, or <c>null</c> to use the registered parameter provider.</param>
        /// <returns><c>true</c> if the command was found and executed.</returns>
        bool Execute(string id, object parameter = null);
        /// <summary>Runs the command bound to a shortcut.</summary>
        /// <param name="shortcut">The shortcut, e.g. "Ctrl+S".</param>
        /// <param name="scope">The scope the shortcut was pressed in.</param>
        /// <param name="parameter">The command parameter, or <c>null</c> to use the registered parameter provider.</param>
        /// <returns><c>true</c> if a command was found and executed.</returns>
        bool TryExecuteShortcut(string shortcut, string scope, object parameter = null);
    }
}
