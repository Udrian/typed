using System.Collections.Generic;

namespace TypeD.View.TreeNodes
{
    /// <summary>A container of child nodes, either a tree or a node.</summary>
    public interface TreeNode
    {
        /// <summary>Gets or sets the child nodes.</summary>
        IList<Node> Nodes { get; set; }
        /// <summary>Adds a child node.</summary>
        /// <param name="name">The displayed name.</param>
        /// <param name="key">The key that identifies the node among its siblings.</param>
        /// <param name="item">The object the node represents.</param>
        void AddNode(string name, string key, object item);
        /// <summary>Checks whether a direct child with the key exists.</summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a child has the key.</returns>
        bool Contains(string key);
        /// <summary>Gets the direct child with the key.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The child, or <c>null</c> if there is none.</returns>
        Node Get(string key);
        /// <summary>Removes all descendant nodes.</summary>
        void Clear();
    }
}
