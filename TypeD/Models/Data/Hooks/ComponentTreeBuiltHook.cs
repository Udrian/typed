using TypeD.View.TreeNodes;

namespace TypeD.Models.Data.Hooks
{
    public class ComponentTreeBuiltHook : Hook
    {
        public Tree Tree { get; private set; }

        public ComponentTreeBuiltHook() { }
        public ComponentTreeBuiltHook(Tree tree)
        {
            Tree = tree;
        }
    }
}
