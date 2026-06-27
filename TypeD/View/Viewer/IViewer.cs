using TypeD.Models.Data;

namespace TypeD.View.Viewer
{
    /// <summary>
    /// Interface for Viewers, which can be used to draw Game or Components
    /// </summary>
    public interface IViewer
    {
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
    }
}
