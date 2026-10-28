using Avalonia.Controls;
using Dock.Model.Core;

namespace TypeD.View
{
    public class Panel
    {
        // Properties
        public string ID { get; set; }
        public string Title { get; set; }
        public Control PanelView { get; set; }
        public bool Open { get; internal set; }
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
