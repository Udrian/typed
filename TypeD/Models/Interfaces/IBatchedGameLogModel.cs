using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>A game log model that accepts and announces messages in batches.</summary>
    public interface IBatchedGameLogModel : IGameLogModel
    {
        /// <summary>Raised once after a batch is added, with whether older entries were trimmed.</summary>
        public event Action<IReadOnlyList<GameLogEntry>, bool> EntriesAdded;

        /// <summary>Adds a group of entries and raises one batch notification.</summary>
        /// <param name="entries">The entries to append.</param>
        public void AddRange(IReadOnlyList<GameLogEntry> entries);
    }
}
