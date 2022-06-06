using System.Collections.Generic;
using System.Windows.Controls;

namespace TypeD.Models.Data.SettingContexts
{
    [Name("MainWindow")]
    public class MainWindowSettingContext : SettingContext
    {
        public class Panel
        {
            public string ID { get; set; }
            public bool Open { get; set; }
            public Dock Dock { get; set; }
            public int Length { get; set; }
            public bool Span { get; set; }
            public string Parent { get; set; }

            public Panel(string id, bool open = false, Dock dock = Dock.Left, int length = 50, bool span = false, string parent = "")
            {
                ID = id;
                Open = open;
                Dock = dock;
                Length = length;
                Span = span;
                Parent = parent;
            }
        }

        public Setting<int> SizeX { get; set; } = new Setting<int>(1024);
        public Setting<int> SizeY { get; set; } = new Setting<int>(768);
        public Setting<bool> Fullscreen { get; set; } = new Setting<bool>(true);
        public Setting<string> ExternalEditor { get; set; } = new Setting<string>("code {path}");
        public Setting<string> ViewerType { get; set; } = new Setting<string>("TypeDCore.View.Viewer.ConsoleViewer");
        public Setting<List<Panel>> Panels { get; set; }

        public MainWindowSettingContext()
        {
            SaveOnExit = true;

            Panels = new Setting<List<Panel>>(new List<Panel>()
            {
                new Panel("typed_tabs", true, Dock.Top, 0, false, ""),
                new Panel("typed_component", true, Dock.Left, 175, false, ""),
                new Panel("typed_output", true, Dock.Bottom, 250, true, ""),
                new Panel("typed_componenttypebrowser", true, Dock.Right, 175, true, "typed_output")
            });
        }
    }
}