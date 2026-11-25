namespace TypeD.Models.Data.Hooks
{
    /// <summary>Fired after a property editor session opened in the property editor panel.</summary>
    public class PropertyEditorOpenedHook : Hook
    {
        /// <summary>Gets or sets the session that opened.</summary>
        public PropertyEditorSession Session { get; set; }
    }

    /// <summary>Fired after a property editor session closed.</summary>
    public class PropertyEditorClosedHook : Hook
    {
        /// <summary>Gets or sets the session that closed.</summary>
        public PropertyEditorSession Session { get; set; }
    }
}
