using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace TypeD.Models.Data
{
    public class Content
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
        public Bitmap Thumbnail { get; protected set; }

        public virtual void Initialize() { }

        /// <summary>
        /// Creates the tooltip shown when hovering the content. Content types override this to show metadata relevant to them.
        /// </summary>
        public virtual Control CreateToolTip()
        {
            var panel = CreateToolTipPanel();
            AddToolTipRows(panel, GetToolTipProperties());
            return panel;
        }

        /// <summary>
        /// The name and value pairs displayed in the tooltip. Content types override this to add their own metadata.
        /// </summary>
        protected virtual List<KeyValuePair<string, string>> GetToolTipProperties()
        {
            var properties = new List<KeyValuePair<string, string>>
            {
                new("File", System.IO.Path.GetFileName(Path)),
                new("Folder", System.IO.Path.GetDirectoryName(Path)),
                new("Size", FormatSize(Data?.LongLength ?? 0))
            };

            if (!string.IsNullOrEmpty(Path) && File.Exists(Path))
                properties.Add(new("Modified", File.GetLastWriteTime(Path).ToString("g")));

            return properties;
        }

        protected static StackPanel CreateToolTipPanel()
        {
            return new StackPanel { Spacing = 6, MaxWidth = 420 };
        }

        protected static void AddToolTipRows(StackPanel panel, IEnumerable<KeyValuePair<string, string>> properties)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };

            var row = 0;
            foreach (var property in properties)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                var name = new TextBlock { Text = property.Key, Opacity = 0.6, Margin = new Thickness(0, 0, 10, 0) };
                Grid.SetRow(name, row);
                grid.Children.Add(name);

                var value = new TextBlock { Text = property.Value, TextWrapping = TextWrapping.Wrap };
                Grid.SetRow(value, row);
                Grid.SetColumn(value, 1);
                grid.Children.Add(value);

                row++;
            }

            panel.Children.Add(grid);
        }

        protected static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            var size = (double)bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return unit == 0 ? $"{bytes} B" : $"{size:0.##} {units[unit]} ({bytes:N0} B)";
        }
    }
}
