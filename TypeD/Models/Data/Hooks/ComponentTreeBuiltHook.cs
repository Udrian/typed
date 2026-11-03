using TypeD.View.TreeNodes;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after the component tree has been built, so listeners can inspect or extend it.
    /// </summary>
    public class ComponentTreeBuiltHook : Hook
    {
        /// <summary>Gets the tree that was built.</summary>
        public Tree Tree { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ComponentTreeBuiltHook"/> class.</summary>
        public ComponentTreeBuiltHook() { }

        /// <summary>Initializes a new instance of the <see cref="ComponentTreeBuiltHook"/> class.</summary>
        /// <param name="tree">The tree that was built.</param>
        public ComponentTreeBuiltHook(Tree tree)
        {
            Tree = tree;
        }
    }
}
