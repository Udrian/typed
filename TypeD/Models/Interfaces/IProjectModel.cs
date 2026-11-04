using System.Threading.Tasks;
using TypeD.Code;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that builds, runs and generates code for projects.</summary>
    public interface IProjectModel : IModel
    {
        /// <summary>Builds the game and its project references into the editor's build output folder.</summary>
        /// <param name="project">The project.</param>
        /// <returns><c>true</c> if the build succeeded.</returns>
        public Task<bool> Build(Project project);
        /// <summary>
        /// Builds the changed modules of a project, then the game. Rebuilt modules that can be reloaded are loaded again together
        /// with the game, which is stopped and started again around the reload.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="changedModules">The modules whose source changed, or <c>null</c> when only the game changed.</param>
        /// <returns><c>true</c> if the build succeeded.</returns>
        public Task<bool> Build(Project project, IEnumerable<Module> changedModules);
        /// <summary>Starts watching a project's files and the files of its local modules for changes.</summary>
        /// <param name="project">The project.</param>
        public void StartWatching(Project project);
        /// <summary>Updates the watched module folders of a watched project after its modules changed.</summary>
        /// <param name="project">The project.</param>
        public void RefreshWatching(Project project);
        /// <summary>Stops watching a project's files for changes.</summary>
        /// <param name="project">The project.</param>
        public void StopWatching(Project project);
        /// <summary>Runs a project.</summary>
        /// <param name="project">The project.</param>
        public void Run(Project project);
        /// <summary>Initializes generated code for a project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="code">The code to initialize.</param>
        public void InitCode(Project project, Codalyzer code);
        /// <summary>Saves generated code to disk.</summary>
        /// <param name="code">The code to save.</param>
        public void SaveCode(Codalyzer code);
        /// <summary>Initializes generated code and saves it to disk.</summary>
        /// <param name="project">The project.</param>
        /// <param name="code">The code to initialize and save.</param>
        public void InitAndSaveCode(Project project, Codalyzer code);
        /// <summary>Builds the component tree of a project.</summary>
        /// <param name="project">The project.</param>
        public void BuildComponentTree(Project project);
        /// <summary>Loads the built assembly of a project.</summary>
        /// <param name="project">The project.</param>
        /// <returns><c>true</c> if the assembly was loaded.</returns>
        public bool LoadAssembly(Project project);
        /// <summary>Transforms a namespace string for a project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="namespace">The namespace to transform.</param>
        /// <returns>The transformed namespace.</returns>
        public string TransformNamespaceString(Project project, string @namespace);
    }
}
