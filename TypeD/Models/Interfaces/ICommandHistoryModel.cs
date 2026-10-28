using System.Collections.ObjectModel;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface ICommandHistoryModel : IModel
    {
        public ObservableCollection<CommandHistoryEntry> Entries { get; }
        public bool CanUndo { get; }
        public bool CanRedo { get; }
        public string CurrentPosition { get; }
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

        public void Record(string commandName, string context, Action undoAction, Action redoAction);
        public bool Undo();
        public bool Redo();
    }
}
