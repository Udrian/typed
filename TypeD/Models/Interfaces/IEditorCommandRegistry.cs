using TypeD.Commands;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface IEditorCommandRegistry : IModel
    {
        IReadOnlyCollection<EditorCommand> Commands { get; }
        IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            Action<object> execute,
            Predicate<object> canExecute = null,
            Func<object> parameterProvider = null);
        IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            CustomCommand customCommand,
            Func<object> parameterProvider = null);
        T GetCommand<T>(string id) where T : CustomCommand;
        bool Execute(string id, object parameter = null);
        bool TryExecuteShortcut(string shortcut, string scope, object parameter = null);
    }
}
