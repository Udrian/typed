using TypeD.ViewModel;

namespace TypeD.Models.Data
{
    /// <summary>
    /// An open edit of a single property in the property editor panel. The editor view and any module
    /// overlay use the session to read the current value and to commit new values.
    /// </summary>
    public class PropertyEditorSession
    {
        /// <summary>Gets the component being edited.</summary>
        public Component Component { get; }
        /// <summary>Gets the property being edited.</summary>
        public Property Property { get; internal set; }
        /// <summary>Gets the property node that commits values through the editor command history.</summary>
        public PropertyNode Node { get; }
        /// <summary>Gets the template that created the editor.</summary>
        public PanelPropertyTemplate Template { get; }
        /// <summary>Gets a value indicating whether the session is still open.</summary>
        public bool IsOpen { get; internal set; } = true;

        /// <summary>Gets the current value of the property.</summary>
        public object Value => Property.Value;

        /// <summary>Raised after the property value changed, from the editor or from anywhere else.</summary>
        public event Action Changed;
        /// <summary>Raised once when the session is closed.</summary>
        public event Action Closed;

        internal Action CloseRequested;

        /// <summary>Initializes a new instance of the <see cref="PropertyEditorSession"/> class.</summary>
        public PropertyEditorSession(Component component, Property property, PropertyNode node, PanelPropertyTemplate template)
        {
            Component = component;
            Property = property;
            Node = node;
            Template = template;
        }

        /// <summary>Commits a new value as an undoable property change. Replace collections instead of mutating them.</summary>
        public void SetValue(object value)
        {
            if (!IsOpen)
                return;
            Node.Value = value;
        }

        /// <summary>Closes the session and the property editor panel.</summary>
        public void Close()
        {
            CloseRequested?.Invoke();
        }

        internal void RaiseChanged() => Changed?.Invoke();
        internal void RaiseClosed() => Closed?.Invoke();
    }
}
