using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace TypeD.Models.Data
{
    public class CommandHistoryEntry : INotifyPropertyChanged
    {
        private static readonly IBrush CurrentHistoryBrush = new SolidColorBrush(Color.FromRgb(59, 93, 70));
        private static readonly IBrush UndoneHistoryBrush = new SolidColorBrush(Color.FromRgb(32, 35, 41));
        private static readonly IBrush AppliedHistoryBrush = new SolidColorBrush(Color.FromRgb(36, 40, 48));
        private bool _isCurrent;
        private bool _isUndone;

        public string CommandName { get; }
        public string Context { get; }
        public string Timestamp { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsCurrent
        {
            get => _isCurrent;
            private set
            {
                if (_isCurrent == value)
                    return;
                _isCurrent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HistoryBackground));
            }
        }

        public bool IsUndone
        {
            get => _isUndone;
            private set
            {
                if (_isUndone == value)
                    return;
                _isUndone = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HistoryBackground));
            }
        }

        public IBrush HistoryBackground => IsCurrent
            ? CurrentHistoryBrush
            : IsUndone ? UndoneHistoryBrush : AppliedHistoryBrush;

        private Action UndoAction;
        private Action RedoAction;

        public CommandHistoryEntry(string commandName, string context, Action undoAction, Action redoAction)
        {
            CommandName = commandName;
            Context = context;
            UndoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
            RedoAction = redoAction ?? throw new ArgumentNullException(nameof(redoAction));
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Adds work that happened after the entry was recorded, so undo and redo cover it as well.
        /// </summary>
        public void Amend(Action undoAction, Action redoAction)
        {
            var previousUndo = UndoAction;
            var previousRedo = RedoAction;
            UndoAction = () => { undoAction(); previousUndo(); };
            RedoAction = () => { previousRedo(); redoAction(); };
        }

        public void Undo()
        {
            UndoAction();
        }

        public void Redo()
        {
            RedoAction();
        }

        public void SetHistoryState(bool isCurrent, bool isUndone)
        {
            IsCurrent = isCurrent;
            IsUndone = isUndone;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
