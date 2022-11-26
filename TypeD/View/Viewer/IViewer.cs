using TypeD.Models.Data;

namespace TypeD.View.Viewer
{
    /// <summary>
    /// Interface for Viewers, which can be used to draw Game or Components
    /// </summary>
    public interface IViewer
    {
        /// <summary>
        /// Init function
        /// </summary>
        /// <param name="project">Loaded Project</param>
        /// <param name="component">Loaded Component</param>
        public void Init(Project project, Component component);
        
        /// <summary>
        /// Loaded component
        /// </summary>
        public Component Component { get; }
    }
}
