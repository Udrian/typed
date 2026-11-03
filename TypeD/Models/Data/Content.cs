using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace TypeD.Models.Data
{
    /// <summary>
    /// A content file (such as an image or font) of a project, with its raw data and optional thumbnail.
    /// </summary>
    public class Content
    {
        /// <summary>Gets or sets the file path of the content.</summary>
        public string Path { get; set; }
        /// <summary>Gets or sets the display name of the content.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the raw bytes of the content.</summary>
        public byte[] Data { get; set; }
        /// <summary>Gets the thumbnail image of the content, if the content type provides one.</summary>
        public Bitmap Thumbnail { get; protected set; }

        /// <summary>Prepares the content after its data is loaded; content types override this to build their thumbnail and metadata.</summary>
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

        /// <summary>Creates the empty vertical panel that hosts the tooltip rows.</summary>
        /// <returns>The new panel.</returns>
        protected static StackPanel CreateToolTipPanel()
        {
            return new StackPanel { Spacing = 6, MaxWidth = 420 };
        }

        /// <summary>Adds a two-column grid of name and value rows to the tooltip panel.</summary>
        /// <param name="panel">The panel to add the grid to.</param>
        /// <param name="properties">The name and value pairs to show.</param>
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

        /// <summary>Formats a byte count as a human readable size, e.g. "1.5 KB (1,536 B)".</summary>
        /// <param name="bytes">The size in bytes.</param>
        /// <returns>The formatted size.</returns>
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
