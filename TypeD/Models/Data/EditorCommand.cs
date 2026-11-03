using TypeD.Commands;

namespace TypeD.Models.Data
{
    /// <summary>Helpers for building editor shortcut strings.</summary>
    public static class EditorShortcut
    {
        /// <summary>Builds a shortcut string such as "Ctrl+Shift+S" from a key and its modifiers.</summary>
        /// <param name="key">The key name.</param>
        /// <param name="control">Whether Ctrl is held.</param>
        /// <param name="shift">Whether Shift is held.</param>
        /// <param name="alt">Whether Alt is held.</param>
        /// <param name="meta">Whether Meta (Windows/Command) is held.</param>
        /// <returns>The shortcut string.</returns>
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

    /// <summary>A named editor action with an optional keyboard shortcut and an execution condition.</summary>
    public sealed class EditorCommand
    {
        /// <summary>Gets the unique id of the command.</summary>
        public string Id { get; }
        /// <summary>Gets the display name of the command.</summary>
        public string Name { get; }
        /// <summary>Gets the description of what the command does.</summary>
        public string Description { get; }
        /// <summary>Gets the scope (area of the editor) the command applies to.</summary>
        public string Scope { get; }
        /// <summary>Gets the shortcut the command was registered with.</summary>
        public string DefaultShortcut { get; }
        /// <summary>Gets or sets the shortcut currently bound to the command.</summary>
        public string Shortcut { get; set; }
        /// <summary>Gets the shortcut text for display, or "No shortcut" when none is bound.</summary>
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

        /// <summary>Determines whether the command can currently run.</summary>
        /// <param name="parameter">The command parameter, or <c>null</c> to use the registered parameter provider.</param>
        /// <returns><c>true</c> if the command can execute.</returns>
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
