using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace TypeD.Models.Data
{
    /// <summary>
    /// One recorded editor command in the command history, with the actions needed to undo and redo it.
    /// </summary>
    public class CommandHistoryEntry : INotifyPropertyChanged
    {
        private static readonly IBrush CurrentHistoryBrush = new SolidColorBrush(Color.FromRgb(59, 93, 70));
        private static readonly IBrush UndoneHistoryBrush = new SolidColorBrush(Color.FromRgb(32, 35, 41));
        private static readonly IBrush AppliedHistoryBrush = new SolidColorBrush(Color.FromRgb(36, 40, 48));
        private bool _isCurrent;
        private bool _isUndone;

        /// <summary>Gets the name of the command.</summary>
        public string CommandName { get; }
        /// <summary>Gets the context the command was executed in, e.g. the affected component.</summary>
        public string Context { get; }
        /// <summary>Gets the time the entry was recorded, formatted as text.</summary>
        public string Timestamp { get; }

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets a value indicating whether this entry is the current position in the history.</summary>
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

        /// <summary>Gets a value indicating whether this entry has been undone.</summary>
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

        /// <summary>Gets the brush used to show the entry's state (current, undone or applied) in the history list.</summary>
        public IBrush HistoryBackground => IsCurrent
            ? CurrentHistoryBrush
            : IsUndone ? UndoneHistoryBrush : AppliedHistoryBrush;

        private Action UndoAction;
        private Action RedoAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHistoryEntry"/> class.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="context">The context the command was executed in.</param>
        /// <param name="undoAction">The action that reverts the command.</param>
        /// <param name="redoAction">The action that reapplies the command.</param>
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

        /// <summary>Reverts the command.</summary>
        public void Undo()
        {
            UndoAction();
        }

        /// <summary>Reapplies the command.</summary>
        public void Redo()
        {
            RedoAction();
        }

        /// <summary>Updates where this entry sits relative to the current history position.</summary>
        /// <param name="isCurrent">Whether the entry is the current position.</param>
        /// <param name="isUndone">Whether the entry has been undone.</param>
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
