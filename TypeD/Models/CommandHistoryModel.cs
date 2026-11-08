using System.Collections.ObjectModel;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class CommandHistoryModel : ICommandHistoryModel
    {
        private int CurrentIndex = -1;

        public void Init(IResourceModel resourceModel)
        {
        }

        public ObservableCollection<CommandHistoryEntry> Entries { get; } = new();
        public bool CanUndo => CurrentIndex >= 0 && CurrentIndex < Entries.Count;
        public bool CanRedo => CurrentIndex > 0;
        public string CurrentPosition => CurrentIndex < 0 || CurrentIndex >= Entries.Count
            ? "Before the first command"
            : Entries[CurrentIndex].CommandName;
        public event EventHandler HistoryChanged;
        public bool IsReplaying { get; private set; }

        public bool AmendCurrent(Action undoAction, Action redoAction)
        {
            if (CurrentIndex != 0 || Entries.Count == 0)
                return false;

            Entries[0].Amend(undoAction, redoAction);
            return true;
        }

        public void Record(string commandName, string context, Action undoAction, Action redoAction)
        {
            while (CurrentIndex > 0)
            {
                Entries.RemoveAt(0);
                CurrentIndex--;
            }

            Entries.Insert(0, new CommandHistoryEntry(commandName, context, undoAction, redoAction));
            CurrentIndex = 0;
            UpdateEntryStates();
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;

            IsReplaying = true;
            try { Entries[CurrentIndex].Undo(); }
            finally { IsReplaying = false; }
            CurrentIndex++;
            UpdateEntryStates();
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo)
                return false;

            var nextIndex = CurrentIndex - 1;
            IsReplaying = true;
            try { Entries[nextIndex].Redo(); }
            finally { IsReplaying = false; }
            CurrentIndex = nextIndex;
            UpdateEntryStates();
            return true;
        }

        private void UpdateEntryStates()
        {
            for (var index = 0; index < Entries.Count; index++)
            {
                Entries[index].SetHistoryState(index == CurrentIndex, index < CurrentIndex);
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}