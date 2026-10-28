using Dock.Model.Core;

namespace TypeD.Models.Data.SettingContexts
{
    public sealed class ShortcutBindingOverrides : Dictionary<string, string>
    {
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

    [Name("MainWindow")]
    public class MainWindowSettingContext : SettingContext
    {
        public Setting<int> SizeX { get; set; } = new Setting<int>(1024);
        public Setting<int> SizeY { get; set; } = new Setting<int>(768);
        public Setting<bool> Fullscreen { get; set; } = new Setting<bool>(true);
        public Setting<string> ExternalEditor { get; set; } = new Setting<string>("code {path}");
        public Setting<string> DockLayout { get; set; } = new Setting<string>(string.Empty);
        public Setting<string> ViewerType { get; set; } = new Setting<string>("TypeDTK.View.Viewer.TKViewer"/*"TypeDCore.View.Viewer.ConsoleViewer"*/); //TODO: Make a better solution than this.
        public Setting<ShortcutBindingOverrides> ShortcutOverrides { get; set; } = new Setting<ShortcutBindingOverrides>(new ShortcutBindingOverrides());

        public MainWindowSettingContext()
        {
            SaveOnExit = true;
        }
    }
}