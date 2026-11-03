using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that downloads, loads and unloads modules.</summary>
    public interface IModuleModel : IModel
    {
        /// <summary>Downloads a module into the module cache.</summary>
        /// <param name="module">The module to download.</param>
        /// <param name="progress">Callback reporting download progress.</param>
        /// <returns><c>true</c> if the module is available after the download.</returns>
        public Task<bool> Download(Module module, Action<long, int, long> progress);
        /// <summary>Loads the assemblies of a module for a project.</summary>
        /// <param name="project">The project the module is used by.</param>
        /// <param name="module">The module to load.</param>
        public void LoadAssembly(Project project, Module module);
        /// <summary>Unloads the assemblies of a module.</summary>
        /// <param name="module">The module to unload.</param>
        public void UnloadAssembly(Module module);
        /// <summary>Adds the module's references to a project's csproj XML.</summary>
        /// <param name="module">The module to add.</param>
        /// <param name="project">The csproj XML.</param>
        public void AddToProjectXML(Module module, XElement project);
        /// <summary>Removes the module's references from a project's csproj XML.</summary>
        /// <param name="module">The module to remove.</param>
        /// <param name="project">The csproj XML.</param>
        public void RemoveFromProjectXML(Module module, XElement project);
    }
}
