using System.Collections.Generic;

namespace TypeD.View.TreeNodes
{
    /// <summary>A tree of nodes.</summary>
    public class Tree : TreeNode
    {
        /// <inheritdoc/>
        public IList<Node> Nodes { get; set; }

        /// <summary>Initializes a new instance of the <see cref="Tree"/> class with no nodes.</summary>
        public Tree()
        {
            Nodes = new List<Node>();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public bool Contains(string key)
        {
            return Get(key) != null;
        }

        /// <inheritdoc/>
        public Node Get(string key)
        {
            foreach(var node in Nodes)
            {
                if(node.Key == key) return node;
            }
            return null;
        }

        /// <inheritdoc/>
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
