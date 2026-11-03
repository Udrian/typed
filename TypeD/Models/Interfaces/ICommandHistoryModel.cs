using System.Collections.ObjectModel;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that records editor commands and supports undo and redo.</summary>
    public interface ICommandHistoryModel : IModel
    {
        /// <summary>Gets the recorded history entries.</summary>
        public ObservableCollection<CommandHistoryEntry> Entries { get; }
        /// <summary>Gets a value indicating whether there is an entry to undo.</summary>
        public bool CanUndo { get; }
        /// <summary>Gets a value indicating whether there is an entry to redo.</summary>
        public bool CanRedo { get; }
        /// <summary>Gets a text describing the current position in the history.</summary>
        public string CurrentPosition { get; }
        /// <summary>Occurs when the history changes (entry recorded, undone or redone).</summary>
        public event EventHandler HistoryChanged;

        /// <summary>
        /// True while an undo or redo is running, so handlers can tell replayed changes from new ones.
        /// </summary>
        public bool IsReplaying { get; }

        /// <summary>
        /// Adds follow-up work to the most recent entry so undoing or redoing it covers the work too.
        /// Returns false if the most recent entry is not the current one.
        /// </summary>
        public bool AmendCurrent(Action undoAction, Action redoAction);

        /// <summary>Records an executed command so it can be undone and redone.</summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="context">The context the command was executed in.</param>
        /// <param name="undoAction">The action that reverts the command.</param>
        /// <param name="redoAction">The action that reapplies the command.</param>
        public void Record(string commandName, string context, Action undoAction, Action redoAction);
        /// <summary>Undoes the current entry.</summary>
        /// <returns><c>true</c> if an entry was undone.</returns>
        public bool Undo();
        /// <summary>Redoes the next undone entry.</summary>
        /// <returns><c>true</c> if an entry was redone.</returns>
        public bool Redo();
    }
}
