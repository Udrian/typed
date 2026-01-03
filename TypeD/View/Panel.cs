using Avalonia.Controls;

namespace TypeD.View
{
    public class Panel
    {
        // Properties
        public string ID { get; set; }
        public string Title { get; set; }
        public Control PanelView { get; set; }
        public bool Open { get; internal set; }

        // Constructors
        internal Panel(string id, string title, Control view)
        { 
            ID = id;
            Title = title;
            PanelView = view;
        }
    }
}
