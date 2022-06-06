using System.Windows;

namespace TypeD.View
{
    public class Panel
    {
        // Properties
        public string ID { get; set; }
        public string Title { get; set; }
        public UIElement PanelView { get; set; }
        public bool Open { get; internal set; }

        // Constructors
        internal Panel(string id, string title, UIElement view)
        { 
            ID = id;
            Title = title;
            PanelView = view;
        }
    }
}
