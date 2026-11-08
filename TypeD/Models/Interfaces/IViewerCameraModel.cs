using TypeD.View.Viewer;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Shares the camera of the active viewer with other parts of the editor, such as the toolbar.
    /// </summary>
    public interface IViewerCameraModel : IModel
    {
        /// <summary>
        /// Gets the camera of the active viewer, or null when no viewer with a camera is loaded.
        /// </summary>
        ViewerCamera Camera { get; }
        /// <summary>
        /// Gets or sets the keyboard navigation speed, applied to every attached camera and saved in the settings.
        /// </summary>
        double Speed { get; set; }

        /// <summary>
        /// Raised when a camera is attached or detached, when the attached camera moves, or when the speed changes.
        /// </summary>
        event Action Changed;

        /// <summary>
        /// Makes a camera the active one and applies the speed to it.
        /// </summary>
        void Attach(ViewerCamera camera);
        /// <summary>
        /// Clears the active camera if it is the specified one.
        /// </summary>
        void Detach(ViewerCamera camera);
    }
}
