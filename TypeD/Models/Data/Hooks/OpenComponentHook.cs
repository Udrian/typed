namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when a component should be opened in the editor.
    /// </summary>
    public class OpenComponentHook : Hook
    {
        /// <summary>Gets or sets the project the component belongs to.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component to open.</summary>
        public Component Component { get; set; }
    }
}
