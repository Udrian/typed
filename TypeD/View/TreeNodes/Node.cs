using System.Collections.Generic;

namespace TypeD.View.TreeNodes
{
    public class Node : TreeNode
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public object Item { get; set; }
        public Tree Tree { get; private set; }
        public Node Parent { get; private set; }
        public IList<Node> Nodes { get; set; }

        internal Node(string name, string key, object item, Tree tree, Node parent)
        {
            Name = name;
            Key = key;
            Item = item;
            Tree = tree;
            Parent = parent;
            Nodes = new List<Node>();
        }

        public void AddNode(string name, string key, object item)
        {
            Tree.AddNode(this, name, key, item);
        }

        public bool Contains(string key)
        {
            return Get(key) != null;
        }

        public Node Get(string key)
        {
            foreach(var node in Nodes)
            {
                if(node.Key == key) return node;
            }
            return null;
        }

        public void Clear()
        {
            Tree.Clear(this);
        }

        internal void InternalClear()
        {
            foreach(var node in Nodes)
            {
                node.InternalClear();
            }
            Nodes.Clear();
        }
    }
}
