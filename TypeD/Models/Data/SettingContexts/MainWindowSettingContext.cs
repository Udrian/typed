using Dock.Model.Core;

namespace TypeD.Models.Data.SettingContexts
{
    [Name("MainWindow")]
    public class MainWindowSettingContext : SettingContext
    {
        public class Panel
        {
            public string ID { get; set; }
            public bool Open { get; set; }
            public Alignment Dock { get; set; }
            public float Length { get; set; }
            public bool Span { get; set; }
            public string Parent { get; set; }

            public Panel(string id, bool open = false, Alignment dock = Alignment.Left, float length = 1, bool span = false, string parent = "")
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
        public Setting<string> ViewerType { get; set; } = new Setting<string>("TypeDTK.View.Viewer.TKViewer"/*"TypeDCore.View.Viewer.ConsoleViewer"*/); //TODO: Make a better solution than this.
        public Setting<List<Panel>> Panels { get; set; }

        public MainWindowSettingContext()
        {
            SaveOnExit = true;

            Panels = new Setting<List<Panel>>(new List<Panel>()
            {
                new Panel("typed_viewer", true, Alignment.Unset, 1, false, ""),
                new Panel("typed_component", true, Alignment.Left, 0.25f, false, "typed_viewer"),
                new Panel("typed_output", true, Alignment.Bottom, 0.3f, true, "typed_viewer"),
                new Panel("typed_componenttypebrowser", true, Alignment.Right, 0.25f, true, "typed_output")
            });
        }
    }
}