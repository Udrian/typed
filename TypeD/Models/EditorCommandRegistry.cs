using TypeD.Commands;
using TypeD.Models.Data;
using TypeD.Models.Data.SettingContexts;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal sealed class EditorCommandRegistry : IEditorCommandRegistry
    {
        private ISettingModel SettingModel;

        private sealed class Registration : IDisposable
        {
            private readonly EditorCommandRegistry Registry;
            private readonly string Id;
            private readonly Guid Token;

            public Registration(EditorCommandRegistry registry, string id, Guid token)
            {
                Registry = registry;
                Id = id;
                Token = token;
            }

            public void Dispose()
            {
                Registry.Unregister(Id, Token);
            }
        }

        private sealed class RegisteredCommand
        {
            public EditorCommand Command { get; }
            public Guid Token { get; }

            public RegisteredCommand(EditorCommand command, Guid token)
            {
                Command = command;
                Token = token;
            }
        }

        private readonly object SyncRoot = new();
        private readonly Dictionary<string, RegisteredCommand> RegisteredCommands = new();

        public IReadOnlyCollection<EditorCommand> Commands
        {
            get
            {
                lock (SyncRoot)
                    return RegisteredCommands.Values.Select(entry => entry.Command).ToList().AsReadOnly();
            }
        }

        public void Init(IResourceModel resourceModel)
        {
            SettingModel = resourceModel.Get<ISettingModel>();
        }

        public IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            Action<object> execute,
            Predicate<object> canExecute = null,
            Func<object> parameterProvider = null)
        {
            return Register(
                id,
                name,
                description,
                scope,
                defaultShortcut,
                execute,
                canExecute,
                parameterProvider,
                null);
        }

        public IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            CustomCommand customCommand,
            Func<object> parameterProvider = null)
        {
            if (customCommand == null)
                throw new ArgumentNullException(nameof(customCommand));

            return Register(
                id,
                name,
                description,
                scope,
                defaultShortcut,
                customCommand.Execute,
                customCommand.CanExecute,
                parameterProvider,
                customCommand);
        }

        private IDisposable Register(
            string id,
            string name,
            string description,
            string scope,
            string defaultShortcut,
            Action<object> execute,
            Predicate<object> canExecute,
            Func<object> parameterProvider,
            CustomCommand registeredCustomCommand)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A command ID is required.", nameof(id));
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            var token = Guid.NewGuid();
            var command = new EditorCommand(
                id,
                name,
                description,
                string.IsNullOrWhiteSpace(scope) ? "global" : scope,
                defaultShortcut ?? string.Empty,
                execute,
                canExecute,
                parameterProvider,
                registeredCustomCommand);

            var shortcutOverrides = SettingModel.GetContext<MainWindowSettingContext>()?.ShortcutOverrides.Value;
            if (shortcutOverrides != null && shortcutOverrides.TryGetValue(id, out var shortcut))
                command.Shortcut = shortcut;

            lock (SyncRoot)
                RegisteredCommands[id] = new RegisteredCommand(command, token);

            return new Registration(this, id, token);
        }

        public T GetCommand<T>(string id) where T : CustomCommand
        {
            lock (SyncRoot)
            {
                if (RegisteredCommands.TryGetValue(id, out var registration))
                    return registration.Command.RegisteredCustomCommand as T;
            }
            return null;
        }

        public bool Execute(string id, object parameter = null)
        {
            RegisteredCommand registration;
            lock (SyncRoot)
            {
                if (!RegisteredCommands.TryGetValue(id, out registration))
                    return false;
            }

            var resolvedParameter = registration.Command.ResolveParameter(parameter);
            if (!registration.Command.CanExecute(resolvedParameter))
                return false;

            registration.Command.ExecuteAction(resolvedParameter);
            return true;
        }

        public bool TryExecuteShortcut(string shortcut, string scope, object parameter = null)
        {
            var normalizedShortcut = NormalizeShortcut(shortcut);
            if (string.IsNullOrEmpty(normalizedShortcut))
                return false;

            var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "global" : scope;
            EditorCommand command;
            object resolvedParameter = null;
            lock (SyncRoot)
            {
                command = null;
                foreach (var candidate in RegisteredCommands.Values.Select(entry => entry.Command))
                {
                    if (!candidate.Scope.Equals(normalizedScope, StringComparison.OrdinalIgnoreCase) ||
                        NormalizeShortcut(candidate.Shortcut) != normalizedShortcut)
                        continue;

                    var candidateParameter = candidate.ResolveParameter(parameter);
                    if (!candidate.CanExecute(candidateParameter))
                        continue;

                    command = candidate;
                    resolvedParameter = candidateParameter;
                    break;
                }
            }

            if (command == null)
                return false;

            command.ExecuteAction(resolvedParameter);
            return true;
        }

        private void Unregister(string id, Guid token)
        {
            lock (SyncRoot)
            {
                if (RegisteredCommands.TryGetValue(id, out var registration) && registration.Token == token)
                    RegisteredCommands.Remove(id);
            }
        }

        private static string NormalizeShortcut(string shortcut)
        {
            return (shortcut ?? string.Empty)
                .Replace("Control", "Ctrl", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty)
                .ToUpperInvariant();
        }
    }
}
