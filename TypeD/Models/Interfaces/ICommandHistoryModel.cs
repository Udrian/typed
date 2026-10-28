using System.Collections.ObjectModel;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface ICommandHistoryModel : IModel
    {
        public ObservableCollection<CommandHistoryEntry> Entries { get; }

        public void Record(string commandName, string context);
    }
}
