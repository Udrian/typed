namespace TypeD.Models.Data.Hooks
{
    /// <summary>Asks a viewer module to position a pasted node at a world-space point.</summary>
    public class NodeClipboardPositionHook : Hook
    {
        /// <summary>Gets or sets the active project.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the parent receiving the pasted node.</summary>
        public Component Parent { get; set; }
        /// <summary>Gets or sets the restored node before it is inserted.</summary>
        public Component Node { get; set; }
        /// <summary>Gets or sets the horizontal world coordinate.</summary>
        public double X { get; set; }
        /// <summary>Gets or sets the vertical world coordinate.</summary>
        public double Y { get; set; }
    }
}