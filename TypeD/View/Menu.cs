using System.Collections.Generic;

namespace TypeD.View
{
    /// <summary>A menu definition made of menu items.</summary>
    public class Menu
    {
        /// <summary>The top level menu items.</summary>
        public List<MenuItem> Items;

        /// <summary>Initializes a new instance of the <see cref="Menu"/> class with no items.</summary>
        public Menu()
        {
            Items = new List<MenuItem>();
        }
    }
}
