using System.Collections.Generic;

namespace TypeD.View.TreeNodes
{
    public class Tree : TreeNode
    {
        public IList<Node> Nodes { get; set; }

        public Tree()
        {
            Nodes = new List<Node>();
        }

        public void AddNode(string name, string key, object item)
        {
            var node = new Node(name, key, item, this, null);
            Nodes.Add(node);
        }

        internal void AddNode(Node parent, string name, string key, object item)
        {
            var node = new Node(name, key, item, this, parent);
            parent.Nodes.Add(node);
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
            InternalClear();
        }

        internal void Clear(Node node)
        {
            node.InternalClear();
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
