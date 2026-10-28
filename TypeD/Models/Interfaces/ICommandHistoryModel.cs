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

        public void Record(string commandName, string context, Action undoAction, Action redoAction);
        public bool Undo();
        public bool Redo();
    }
}
