using System;
using System.Collections.Generic;

namespace TypeD.View
{
    public class MenuItem
    {
        // Properties
        public string Name { get; set; }
        public Action<object> Click { get; set; }
        public string ClickParameter { get; set; }
        public List<MenuItem> Items;

        // Constructors
        public MenuItem(string name = "", Action<object> click = null, string clickParameter = "", List<MenuItem> items = null)
        {
            Name = name;
            Click = click;
            ClickParameter = clickParameter;
            Items = items ?? new List<MenuItem>();
        }
    }
}
