using TypeD.Models.Data;

namespace TypeD.Models
{
    /// <summary>
    /// What is currently being dragged from a panel towards the viewer or a property. The drag and drop runs in-process,
    /// so the payload is shared here instead of being serialized into the drag data.
    /// </summary>
    public static class ViewerDragPayload
    {
        /// <summary>
        /// A component type dragged from the component browser.
        /// </summary>
        public static Component Component { get; private set; }

        /// <summary>
        /// The full path of a content file dragged from the content panel.
        /// </summary>
        public static string ContentPath { get; private set; }

        /// <summary>Gets whether a component or content file is being dragged.</summary>
        public static bool HasPayload => Component != null || ContentPath != null;

        /// <summary>Sets the dragged component and clears any content payload.</summary>
        public static void SetComponent(Component component)
        {
            Clear();
            Component = component;
        }

        /// <summary>Sets the dragged content path and clears any component payload.</summary>
        public static void SetContent(string path)
        {
            Clear();
            ContentPath = path;
        }

        /// <summary>Clears the shared drag payload.</summary>
        public static void Clear()
        {
            Component = null;
            ContentPath = null;
        }
    }
}
