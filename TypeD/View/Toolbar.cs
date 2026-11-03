using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeD.View
{
    /// <summary>A button in a toolbar group.</summary>
    public class ToolbarItem
    {
        // Properties
        /// <summary>Gets or sets the item name, used as the button text when there is no icon.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the tooltip text.</summary>
        public string ToolTip { get; set; }
        /// <summary>Asset uri of an image, for example avares://TypeDitor/Icons/Run.png</summary>
        public string IconPath { get; set; }
        /// <summary>Avalonia path markup (24x24 grid) used when no IconPath is given.</summary>
        public string IconData { get; set; }
        /// <summary>Toggle items are mutually exclusive within their group.</summary>
        public bool IsToggle { get; set; }
        /// <summary>Gets or sets a function that tells whether a toggle item is currently checked.</summary>
        public Func<bool> IsChecked { get; set; }
        /// <summary>Gets or sets a function that tells whether the item is currently enabled.</summary>
        public Func<bool> IsEnabled { get; set; }
        /// <summary>Gets or sets the action run when the item is clicked.</summary>
        public Action<object> Click { get; set; }
        /// <summary>Gets or sets the name of the view model property whose value is passed to <see cref="Click"/>.</summary>
        public string ClickParameter { get; set; }
    }

    /// <summary>A named group of toolbar items that can be reordered as a unit.</summary>
    public class ToolbarGroup
    {
        /// <summary>Gets or sets the group name.</summary>
        public string Name { get; set; }
        /// <summary>Gets the items of the group.</summary>
        public List<ToolbarItem> Items { get; } = new List<ToolbarItem>();

        /// <summary>Initializes a new instance of the <see cref="ToolbarGroup"/> class.</summary>
        /// <param name="name">The group name.</param>
        public ToolbarGroup(string name)
        {
            Name = name;
        }
    }

    /// <summary>A toolbar made of groups of items.</summary>
    public class Toolbar
    {
        /// <summary>Gets the groups of the toolbar.</summary>
        public List<ToolbarGroup> Groups { get; } = new List<ToolbarGroup>();

        /// <summary>Gets a group by name, creating it if it does not exist.</summary>
        /// <param name="name">The group name.</param>
        /// <returns>The group.</returns>
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

        /// <summary>Adds an item to a group, creating the group if needed.</summary>
        /// <param name="group">The group name.</param>
        /// <param name="item">The item to add.</param>
        public void Add(string group, ToolbarItem item)
        {
            GetGroup(group).Items.Add(item);
        }
    }
}
