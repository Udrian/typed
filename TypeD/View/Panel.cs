using Avalonia.Controls;
using Dock.Model.Core;

namespace TypeD.View
{
    /// <summary>A dockable panel registered with the editor.</summary>
    public class Panel
    {
        // Properties
        /// <summary>Gets or sets the unique panel id.</summary>
        public string ID { get; set; }
        /// <summary>Gets or sets the panel title.</summary>
        public string Title { get; set; }
        /// <summary>Gets or sets the control shown in the panel.</summary>
        public Control PanelView { get; set; }
        /// <summary>Gets a value indicating whether the panel is open.</summary>
        public bool Open { get; internal set; }
        /// <summary>Gets where the panel is docked by default.</summary>
        public Alignment DefaultDockPosition { get; internal set; }

        // Constructors
        internal Panel(string id, string title, Control view, Alignment defaultDockPosition)
        { 
            ID = id;
            Title = title;
            PanelView = view;
            DefaultDockPosition = defaultDockPosition;
        }
    }
}
