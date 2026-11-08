using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Node snapshots shared with the system clipboard and inserted through undoable editor operations.</summary>
    public interface INodeClipboardModel : IModel
    {
        /// <summary>Reads the current system clipboard, clearing stale node availability.</summary>
        Task RefreshAsync();
        /// <summary>Returns whether the source is copyable.</summary>
        bool CanCopy(NodeClipboardCommandData data);
        /// <summary>Returns whether a duplicate can be inserted after its source.</summary>
        bool CanDuplicate(NodeClipboardCommandData data);
        /// <summary>Returns whether cached clipboard content can be pasted under this node.</summary>
        bool CanPaste(NodeClipboardCommandData data);
        /// <summary>Writes an independent node snapshot to the system clipboard.</summary>
        Task CopyAsync(NodeClipboardCommandData data);
        /// <summary>Reads and pastes the current system clipboard.</summary>
        Task PasteAsync(NodeClipboardCommandData data);
        /// <summary>Duplicates a node immediately after its source without changing the clipboard.</summary>
        void Duplicate(NodeClipboardCommandData data);
    }
}
