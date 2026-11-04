using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that tracks and performs pending saves.</summary>
    public interface ISaveModel : IModel
    {
        /// <summary>Gets a value indicating whether there are pending saves.</summary>
        public bool AnythingToSave { get; }
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
    }
}
