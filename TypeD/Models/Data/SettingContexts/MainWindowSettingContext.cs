using Dock.Model.Core;

namespace TypeD.Models.Data.SettingContexts
{
    /// <summary>Shortcut bindings that differ from the defaults, keyed by command id.</summary>
    public sealed class ShortcutBindingOverrides : Dictionary<string, string>
    {
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not ShortcutBindingOverrides other || Count != other.Count)
                return false;

            foreach (var entry in this)
            {
                if (!other.TryGetValue(entry.Key, out var shortcut) ||
                    !string.Equals(entry.Value, shortcut, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var entry in this)
            {
                hash ^= StringComparer.Ordinal.GetHashCode(entry.Key) ^
                        StringComparer.Ordinal.GetHashCode(entry.Value ?? string.Empty);
            }

            return hash;
        }
    }

    /// <summary>Settings of the main window.</summary>
    [Name("MainWindow")]
    public class MainWindowSettingContext : SettingContext
    {
        /// <summary>Gets or sets the window width.</summary>
        public Setting<int> SizeX { get; set; } = new Setting<int>(1024);
        /// <summary>Gets or sets the window height.</summary>
        public Setting<int> SizeY { get; set; } = new Setting<int>(768);
        /// <summary>Gets or sets whether the window starts in fullscreen.</summary>
        public Setting<bool> Fullscreen { get; set; } = new Setting<bool>(true);
        /// <summary>Gets or sets the command used to open files in an external editor; {path} is replaced by the file path.</summary>
        public Setting<string> ExternalEditor { get; set; } = new Setting<string>("code {path}");
        /// <summary>Gets or sets the serialized dock layout.</summary>
        public Setting<string> DockLayout { get; set; } = new Setting<string>(string.Empty);
        /// <summary>Gets or sets the order of the toolbar groups.</summary>
        public Setting<string> ToolbarGroupOrder { get; set; } = new Setting<string>(string.Empty);
        /// <summary>Gets or sets the keyboard navigation speed of the viewer camera, in viewer units per second.</summary>
        public Setting<double> CameraSpeed { get; set; } = new Setting<double>(TypeD.View.Viewer.ViewerCamera.DefaultSpeed);
        /// <summary>Gets or sets the type name of the viewer used to render components.</summary>
        public Setting<string> ViewerType { get; set; } = new Setting<string>("TypeDTK.View.Viewer.TKViewer"/*"TypeDCore.View.Viewer.ConsoleViewer"*/); //TODO: Make a better solution than this.
        /// <summary>Gets or sets the shortcut bindings that differ from the defaults.</summary>
        public Setting<ShortcutBindingOverrides> ShortcutOverrides { get; set; } = new Setting<ShortcutBindingOverrides>(new ShortcutBindingOverrides());

        /// <summary>Initializes a new instance of the <see cref="MainWindowSettingContext"/> class; it is saved on exit.</summary>
        public MainWindowSettingContext()
        {
            SaveOnExit = true;
        }
    }
}