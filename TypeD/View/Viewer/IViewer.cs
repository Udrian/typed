using TypeD.Models.Data;

namespace TypeD.View.Viewer
{
    /// <summary>
    /// Interface for Viewers, which can be used to draw Game or Components
    /// </summary>
    public interface IViewer
    {
        /// <summary>
        /// Gets or sets the project the viewer belongs to.
        /// </summary>
        Project Project { get; set; }

        /// <summary>
        /// Initializes the game into the viewer. This is called when the viewer is loaded, and should be called before Load() or Unload().
        /// </summary>
        public void Init();

        /// <summary>
        /// Load a component into the game to be displayed in the viewer.
        /// </summary>
        /// <param name="component">Component to load</param>
        public void Load(Component component);

        /// <summary>
        /// Unload the component from the game.
        /// </summary>
        public void Unload();

        /// <summary>
        /// Loaded component
        /// </summary>
        public Component Component { get; }

        /// <summary>
        /// The editor camera used to navigate the viewer, or <c>null</c> if the viewer can't be navigated.
        /// Viewer coordinates must be converted with it to get world coordinates.
        /// </summary>
        public ViewerCamera Camera => null;
    }
}
