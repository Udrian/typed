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
    }
}
