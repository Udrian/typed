using System.Collections.Generic;

namespace TypeD.View.TreeNodes
{
    /// <summary>A node of a <see cref="Tree"/>.</summary>
    public class Node : TreeNode
    {
        /// <summary>Gets or sets the displayed name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the key that identifies the node among its siblings.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the object the node represents.</summary>
        public object Item { get; set; }
        /// <summary>Gets the tree the node belongs to.</summary>
        public Tree Tree { get; private set; }
        /// <summary>Gets the parent node, or <c>null</c> for a root node.</summary>
        public Node Parent { get; private set; }
        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void AddNode(string name, string key, object item)
        {
            Tree.AddNode(this, name, key, item);
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
