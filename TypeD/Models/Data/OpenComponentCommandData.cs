namespace TypeD.Models.Data
{
    /// <summary>Identifies the project and component to open in the editor.</summary>
    public class OpenComponentCommandData
    {
        /// <summary>Gets or sets the project being edited.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the component, or null to show the picker.</summary>
        public Component Component { get; set; }
    }
}
