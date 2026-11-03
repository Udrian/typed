namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when a component's editor tab should be closed.
    /// </summary>
    public class CloseComponentHook : Hook
    {
        /// <summary>Gets or sets the project the component belongs to.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component to close.</summary>
        public Component Component { get; set; }
    }
}
