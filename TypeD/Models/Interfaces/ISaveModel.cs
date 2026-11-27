using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that tracks and performs pending saves.</summary>
    public interface ISaveModel : IModel
    {
        /// <summary>Raised when the model's pending-save state changes.</summary>
        event Action<bool> SaveStateChanged;
        /// <summary>Records a completed autosave and notifies listeners without clearing pending saves.</summary>
        void NotifyAutosaveCompleted(Project project);
        /// <summary>Gets a value indicating whether there are pending saves.</summary>
        public bool AnythingToSave { get; }
        /// <summary>Excludes saves and autosaves while applying an immediate file transaction.</summary>
        /// <returns>A lease that releases the save gate when disposed.</returns>
        /// <exception cref="InvalidOperationException">A save is in progress or there are unsaved changes.</exception>
        IDisposable BeginExclusiveUpdate();
        /// <summary>Queues a save context to be saved later.</summary>
        /// <typeparam name="T">The save context type.</typeparam>
        /// <param name="param">The context parameter.</param>
        public void AddSave<T>(object param = null) where T : SaveContext, new();
        /// <summary>Saves a context immediately.</summary>
        /// <typeparam name="T">The save context type.</typeparam>
        /// <param name="project">The project.</param>
        /// <param name="param">The context parameter.</param>
        public void SaveNow<T>(Project project, object param = null) where T : SaveContext, new();
        /// <summary>Checks whether a save context is queued.</summary>
        /// <typeparam name="T">The save context type.</typeparam>
        /// <returns><c>true</c> if it is queued.</returns>
        public bool SaveContextExists<T>() where T : SaveContext, new();
        /// <summary>Gets the queued save context, creating it if needed.</summary>
        /// <typeparam name="T">The save context type.</typeparam>
        /// <param name="param">The context parameter.</param>
        /// <returns>The save context.</returns>
        public T GetSaveContext<T>(object param = null) where T : SaveContext, new();
        /// <summary>Performs all queued saves, including saves queued by save callbacks.</summary>
        /// <param name="project">The project.</param>
        public Task Save(Project project);
        /// <summary>Creates a project copy and writes all pending save contexts into it without clearing them.</summary>
        /// <param name="project">The open project.</param>
        /// <param name="snapshotPath">The destination folder for the autosave snapshot.</param>
        public Task CreateAutosave(Project project, string snapshotPath);
    }
}
