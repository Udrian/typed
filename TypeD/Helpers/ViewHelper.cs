using TypeD.View;
using TypeD.ViewModel;

namespace TypeD.Helpers
{
    public static class ViewHelper
    {
        public static void InitMenu(Avalonia.Controls.ItemsControl currentMenu, MenuItem item, ViewModelBase model)
        {
            Avalonia.Controls.MenuItem newMenuItem = null;
            foreach (var i in currentMenu.Items)
            {
                var mi = i as Avalonia.Controls.MenuItem;
                if ((string)mi.Header == item.Name)
                {
                    newMenuItem = mi;
                    break;
                }
            }

            if (newMenuItem == null)
            {
                newMenuItem = new Avalonia.Controls.MenuItem() { Header = item.Name };
                if (item.Click != null)
                {
                    newMenuItem.Click += (object sender, Avalonia.Interactivity.RoutedEventArgs e) =>
                    {
                        object param = null;
                        if (!string.IsNullOrEmpty(item.ClickParameter))
                        {
                            var type = model.GetType();
                            param = type.GetProperties().FirstOrDefault(p => p.Name == item.ClickParameter).GetValue(model);
                        }
                        item.Click(param);
                    };
                }
                currentMenu.Items.Add(newMenuItem);
            }
            currentMenu = newMenuItem;

            foreach (var menu in item.Items)
            {
                InitMenu(currentMenu, menu, model);
            }
        }

        /// <summary>
        /// Builds the toolbar groups into the panel. Groups get a grip that can be dragged to reorder them;
        /// onOrderChanged is called with the group names in their new order.
        /// </summary>
        public static void InitToolbar(Avalonia.Controls.Panel panel, Toolbar toolbar, ViewModelBase model, IEnumerable<string> groupOrder = null, Action<IReadOnlyList<string>> onOrderChanged = null)
        {
            panel.Children.Clear();

            var order = groupOrder?.ToList() ?? new List<string>();
            var groups = toolbar.Groups
                .Where(g => g.Items.Count > 0)
                .Select((g, index) => new { Group = g, Index = index })
                .OrderBy(g => { var i = order.IndexOf(g.Group.Name); return i < 0 ? int.MaxValue : i; })
                .ThenBy(g => g.Index)
                .Select(g => g.Group);

            foreach (var group in groups)
            {
                panel.Children.Add(CreateToolbarGroup(panel, group, model, onOrderChanged));
            }
        }

        private static Avalonia.Controls.Control CreateToolbarGroup(Avalonia.Controls.Panel panel, ToolbarGroup group, ViewModelBase model, Action<IReadOnlyList<string>> onOrderChanged)
        {
            var buttons = new List<Avalonia.Controls.Primitives.ToggleButton>();
            var items = new Avalonia.Controls.StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 2 };
            foreach (var item in group.Items)
            {
                items.Children.Add(CreateToolbarButton(item, group, model, buttons));
            }

            var grip = new Avalonia.Controls.Border()
            {
                Width = 10,
                Background = Avalonia.Media.Brushes.Transparent,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeAll),
                Child = new Avalonia.Controls.TextBlock()
                {
                    Text = "⋮",
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Opacity = 0.6,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            Avalonia.Controls.ToolTip.SetTip(grip, $"Drag to move the {group.Name} toolbar");

            var container = new Avalonia.Controls.Border()
            {
                Tag = group.Name,
                Margin = new Avalonia.Thickness(2, 2, 6, 2),
                Padding = new Avalonia.Thickness(0, 0, 4, 0),
                CornerRadius = new Avalonia.CornerRadius(3),
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = Avalonia.Media.Brushes.Gray,
                Child = new Avalonia.Controls.StackPanel()
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Children = { grip, items }
                }
            };

            AttachToolbarDrag(panel, grip, container, onOrderChanged);
            return container;
        }

        private static Avalonia.Controls.Control CreateToolbarButton(ToolbarItem item, ToolbarGroup group, ViewModelBase model, List<Avalonia.Controls.Primitives.ToggleButton> toggles)
        {
            Avalonia.Controls.Button button;
            if (item.IsToggle)
            {
                var toggle = new Avalonia.Controls.Primitives.ToggleButton() { IsChecked = item.IsChecked?.Invoke() ?? false };
                toggles.Add(toggle);
                button = toggle;
            }
            else
            {
                button = new Avalonia.Controls.Button();
            }

            button.Padding = new Avalonia.Thickness(5);
            button.IsEnabled = item.IsEnabled?.Invoke() ?? true;
            Avalonia.Controls.ToolTip.SetTip(button, item.ToolTip ?? item.Name);

            var icon = CreateToolbarIcon(item);
            if (icon != null)
            {
                button.Content = icon;
            }
            else
            {
                button.Content = item.Name;
            }

            button.Click += (sender, e) =>
            {
                if (item.IsToggle)
                {
                    foreach (var other in toggles)
                        other.IsChecked = ReferenceEquals(other, button);
                }

                if (item.Click == null)
                    return;

                object param = null;
                if (!string.IsNullOrEmpty(item.ClickParameter))
                {
                    param = model.GetType().GetProperties().FirstOrDefault(p => p.Name == item.ClickParameter)?.GetValue(model);
                }
                item.Click(param);
            };

            return button;
        }

        private static Avalonia.Controls.Control CreateToolbarIcon(ToolbarItem item)
        {
            if (!string.IsNullOrEmpty(item.IconPath))
            {
                try
                {
                    using var stream = Avalonia.Platform.AssetLoader.Open(new Uri(item.IconPath));
                    return new Avalonia.Controls.Image()
                    {
                        Source = new Avalonia.Media.Imaging.Bitmap(stream),
                        Width = 16,
                        Height = 16
                    };
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError($"Could not load toolbar icon {item.IconPath}: {exception}");
                }
            }

            if (!string.IsNullOrEmpty(item.IconData))
            {
                return new Avalonia.Controls.PathIcon()
                {
                    Data = Avalonia.Media.Geometry.Parse(item.IconData),
                    Width = 16,
                    Height = 16
                };
            }

            return null;
        }

        private static void AttachToolbarDrag(Avalonia.Controls.Panel panel, Avalonia.Controls.Control grip, Avalonia.Controls.Control container, Action<IReadOnlyList<string>> onOrderChanged)
        {
            var dragging = false;

            grip.PointerPressed += (sender, e) =>
            {
                if (!e.GetCurrentPoint(grip).Properties.IsLeftButtonPressed)
                    return;
                dragging = true;
                container.Opacity = 0.6;
                e.Pointer.Capture(grip);
                e.Handled = true;
            };

            grip.PointerMoved += (sender, e) =>
            {
                if (!dragging)
                    return;

                var x = e.GetPosition(panel).X;
                var index = panel.Children.IndexOf(container);
                var target = index;
                for (var i = 0; i < panel.Children.Count; i++)
                {
                    var bounds = panel.Children[i].Bounds;
                    if (x >= bounds.Left && x < bounds.Right)
                    {
                        target = i;
                        break;
                    }
                }

                if (target != index)
                {
                    panel.Children.RemoveAt(index);
                    panel.Children.Insert(target, container);
                }
                e.Handled = true;
            };

            void EndDrag(Avalonia.Input.PointerEventArgs e)
            {
                if (!dragging)
                    return;
                dragging = false;
                container.Opacity = 1;
                e.Pointer.Capture(null);
                onOrderChanged?.Invoke(panel.Children.Select(c => (string)c.Tag).ToList());
            }

            grip.PointerReleased += (sender, e) => EndDrag(e);
            grip.PointerCaptureLost += (sender, e) =>
            {
                if (dragging)
                {
                    dragging = false;
                    container.Opacity = 1;
                    onOrderChanged?.Invoke(panel.Children.Select(c => (string)c.Tag).ToList());
                }
            };
        }
    }
}
