using TypeD.View.TreeNodes;
using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    public class ComponentTypeBrowserContextMenuOpenedHook : Hook
    {
        public Menu Menu { get; set; }
        public Node Node { get; private set; }

        public ComponentTypeBrowserContextMenuOpenedHook() { }
        public ComponentTypeBrowserContextMenuOpenedHook(Node node)
        {
            Menu = new Menu();
            Node = node;
        }
    }
}
