using TypeD.View.TreeNodes;
using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the context menu of a node in the component type browser is opened, so listeners can add menu items.
    /// </summary>
    public class ComponentTypeBrowserContextMenuOpenedHook : Hook
    {
        /// <summary>Gets or sets the menu that listeners add their items to.</summary>
        public Menu Menu { get; set; }
        /// <summary>Gets the browser node the context menu was opened on.</summary>
        public Node Node { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ComponentTypeBrowserContextMenuOpenedHook"/> class.</summary>
        public ComponentTypeBrowserContextMenuOpenedHook() { }

        /// <summary>Initializes a new instance of the <see cref="ComponentTypeBrowserContextMenuOpenedHook"/> class with an empty menu.</summary>
        /// <param name="node">The browser node the menu was opened on.</param>
        public ComponentTypeBrowserContextMenuOpenedHook(Node node)
        {
            Menu = new Menu();
            Node = node;
        }
    }
}
