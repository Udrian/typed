using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    public class ComponentContextMenuHook : Hook
    {
        public Project Project { get; set; }
        public Component OpenedComponent { get; set; }
        public Component SelectedComponent { get; set; }
        public Menu Menu { get; set; }
    }
}
