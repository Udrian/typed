using TypeD.Models.Data;

namespace TypeD.Models.Data
{
    /// <summary>The selected source or destination node, and optional viewer-space paste location.</summary>
    public class NodeClipboardCommandData
    {
        /// <summary>Gets or sets the active project.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the node to copy/duplicate or the parent to paste into.</summary>
        public Component Node { get; set; }
        /// <summary>Gets or sets the viewer world position, or null to preserve the copied position.</summary>
        public (double X, double Y)? WorldPosition { get; set; }
    }
}
