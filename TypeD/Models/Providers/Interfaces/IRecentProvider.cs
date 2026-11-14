using System.Collections.Generic;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    /// <summary>Provider of recently opened projects.</summary>
    public interface IRecentProvider : IProvider
    {
        /// <summary>Adds a project to the recent list.</summary>
        /// <param name="projectFilePath">The project file path.</param>
        /// <param name="projectName">The project name.</param>
        public void Add(string projectFilePath, string projectName);
        /// <summary>Updates a recent entry after its project file or name changes.</summary>
        /// <param name="oldProjectFilePath">The previous project file path.</param>
        /// <param name="newProjectFilePath">The current project file path.</param>
        /// <param name="projectName">The current project name.</param>
        public void Rename(string oldProjectFilePath, string newProjectFilePath, string projectName);
        /// <summary>Removes a project from the recent list.</summary>
        /// <param name="projectFilePath">The project file path.</param>
        public void Remove(string projectFilePath);
        /// <summary>Gets the recent projects.</summary>
        /// <returns>The recent entries.</returns>
        public IEnumerable<Recent> Get();
    }
}
