using System;
using System.Collections.Generic;

namespace TypeD.View
{
    /// <summary>A menu entry with an optional click action and sub items.</summary>
    public class MenuItem
    {
        // Properties
        /// <summary>Gets or sets the displayed name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the action run when the item is clicked.</summary>
        public Action<object> Click { get; set; }
        /// <summary>Gets or sets the name of the view model property whose value is passed to <see cref="Click"/>.</summary>
        public string ClickParameter { get; set; }
        /// <summary>Gets or sets whether this entry is enabled when the menu is built.</summary>
        public bool IsEnabled { get; set; } = true;
        /// <summary>The sub items.</summary>
        public List<MenuItem> Items;

        // Constructors
        /// <summary>Initializes a new instance of the <see cref="MenuItem"/> class.</summary>
        /// <param name="name">The displayed name.</param>
        /// <param name="click">The action run when the item is clicked.</param>
        /// <param name="clickParameter">The name of the view model property passed to <paramref name="click"/>.</param>
        /// <param name="items">The sub items, or <c>null</c> for none.</param>
        public MenuItem(string name = "", Action<object> click = null, string clickParameter = "", List<MenuItem> items = null)
        {
            Name = name;
            Click = click;
            ClickParameter = clickParameter;
            Items = items ?? new List<MenuItem>();
        }
    }
}
