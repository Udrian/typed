using System.Threading.Tasks;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Base class for a unit of data that is collected during editing and written to disk when saving.
    /// </summary>
    public abstract class SaveContext
    {
        // Properties
        internal bool ShouldSave { get; set; }

        // Constructors
        /// <summary>Initializes the context before use.</summary>
        /// <param name="resourceModel">The resource model providing access to project resources.</param>
        /// <param name="param">Optional context specific parameter.</param>
        public abstract void Init(IResourceModel resourceModel, object param = null);

        // Functions
        /// <summary>Writes the collected data of the project to disk.</summary>
        /// <param name="project">The project being saved.</param>
        /// <returns>A task that completes when saving is done.</returns>
        public abstract Task SaveAction(Project project);

        /// <summary>Writes this context's pending data into an autosave snapshot.</summary>
        /// <param name="project">The open project.</param>
        /// <param name="snapshotProject">The project copy being autosaved.</param>
        /// <returns>A task that completes when the snapshot data is written.</returns>
        public virtual Task SaveAutosaveAction(Project project, Project snapshotProject)
        {
            throw new NotSupportedException($"{GetType().Name} does not support autosaving.");
        }
    }
}
