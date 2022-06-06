using System.Collections.Generic;

namespace TypeD.View.TreeNodes
{
    public interface TreeNode
    {
        IList<Node> Nodes { get; set; }
        void AddNode(string name, string key, object item);
        bool Contains(string key);
        Node Get(string key);
        void Clear();
    }
}
