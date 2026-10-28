using TypeD.Commands;

namespace TypeD.Models.Data
{
    public static class EditorShortcut
    {
        public static string FromKey(string key, bool control, bool shift, bool alt, bool meta)
        {
            var parts = new List<string>();
            if (control) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            if (meta) parts.Add("Meta");
            parts.Add(key);
            return string.Join("+", parts);
        }
    }

    public sealed class EditorCommand
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Scope { get; }
        public string DefaultShortcut { get; }
        public string Shortcut { get; set; }
        public string ShortcutDisplay => string.IsNullOrWhiteSpace(Shortcut) ? "No shortcut" : Shortcut;

        internal Action<object> ExecuteAction { get; }
        internal Predicate<object> CanExecuteAction { get; }
        internal Func<object> ParameterProvider { get; }
        internal CustomCommand RegisteredCustomCommand { get; }

        internal EditorCommand(
            string id,
            string name,
            string description,
            string scope,
            string shortcut,
            Action<object> execute,
            Predicate<object> canExecute,
            Func<object> parameterProvider,
            CustomCommand registeredCustomCommand = null)
        {
            Id = id;
            Name = name;
            Description = description;
            Scope = scope;
            DefaultShortcut = shortcut;
            Shortcut = shortcut;
            ExecuteAction = execute;
            CanExecuteAction = canExecute;
            ParameterProvider = parameterProvider;
            RegisteredCustomCommand = registeredCustomCommand;
        }

        public bool CanExecute(object parameter = null)
        {
            parameter ??= ParameterProvider?.Invoke();
            return CanExecuteAction?.Invoke(parameter) ?? true;
        }

        internal object ResolveParameter(object parameter)
        {
            return parameter ?? ParameterProvider?.Invoke();
        }
    }
}
