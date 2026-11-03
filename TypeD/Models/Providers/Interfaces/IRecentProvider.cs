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
        /// <summary>Gets the recent projects.</summary>
        /// <returns>The recent entries.</returns>
        public IEnumerable<Recent> Get();
    }
}
