using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeD.View
{
    public class ToolbarItem
    {
        // Properties
        public string Name { get; set; }
        public string ToolTip { get; set; }
        /// <summary>Asset uri of an image, for example avares://TypeDitor/Icons/Run.png</summary>
        public string IconPath { get; set; }
        /// <summary>Avalonia path markup (24x24 grid) used when no IconPath is given.</summary>
        public string IconData { get; set; }
        /// <summary>Toggle items are mutually exclusive within their group.</summary>
        public bool IsToggle { get; set; }
        public Func<bool> IsChecked { get; set; }
        public Func<bool> IsEnabled { get; set; }
        public Action<object> Click { get; set; }
        public string ClickParameter { get; set; }
    }

    public class ToolbarGroup
    {
        public string Name { get; set; }
        public List<ToolbarItem> Items { get; } = new List<ToolbarItem>();

        public ToolbarGroup(string name)
        {
            Name = name;
        }
    }

    public class Toolbar
    {
        public List<ToolbarGroup> Groups { get; } = new List<ToolbarGroup>();

        public ToolbarGroup GetGroup(string name)
        {
            var group = Groups.FirstOrDefault(g => g.Name == name);
            if (group == null)
            {
                group = new ToolbarGroup(name);
                Groups.Add(group);
            }
            return group;
        }

        public void Add(string group, ToolbarItem item)
        {
            GetGroup(group).Items.Add(item);
        }
    }
}
