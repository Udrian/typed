using System;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that runs registered restore methods for a project.</summary>
    public interface IRestoreModel : IModel
    {
        /// <summary>Registers a restore method.</summary>
        /// <param name="restore">The method to run on restore.</param>
        public void AddRestoreMethod(Action<Project> restore);
        /// <summary>Runs all registered restore methods.</summary>
        /// <param name="project">The project to restore.</param>
        public Task Restore(Project project);
    }
}
