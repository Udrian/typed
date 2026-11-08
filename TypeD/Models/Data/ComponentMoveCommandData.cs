namespace TypeD.Models.Data
{
    /// <summary>Specifies where a node is placed relative to its target.</summary>
    public enum MovePosition
    {
        /// <summary>Places the node before the target.</summary>
        Before,
        /// <summary>Places the node after the target.</summary>
        After,
        /// <summary>Appends the node to the target's children.</summary>
        Child,
        /// <summary>Places the node first among the target's children.</summary>
        FirstChild
    }

    /// <summary>Describes a single node move.</summary>
    public class ComponentMove
    {
        /// <summary>Gets or sets the node to move.</summary>
        public Component Node { get; set; }
        /// <summary>Gets or sets the destination node.</summary>
        public Component Target { get; set; }
        /// <summary>Gets or sets the placement relative to the target.</summary>
        public MovePosition Position { get; set; }
    }

    /// <summary>Describes an undoable editor move operation.</summary>
    public class ComponentMoveCommandData
    {
        /// <summary>Gets or sets the project being edited.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the primary node to move.</summary>
        public Component Node { get; set; }
        /// <summary>
        /// All nodes to move together with <see cref="Node"/>. When null only <see cref="Node"/> is moved.
        /// </summary>
        public IReadOnlyList<Component> Nodes { get; set; }
        /// <summary>
        /// Explicit moves, executed in order as a single undoable step. Overrides Node, Nodes, Target and Position.
        /// </summary>
        public IReadOnlyList<ComponentMove> Moves { get; set; }
        /// <summary>
        /// The node to move relative to. When null a dialog asks for the target.
        /// </summary>
        public Component Target { get; set; }
        /// <summary>Gets or sets the placement relative to the target.</summary>
        public MovePosition Position { get; set; } = MovePosition.Child;
    }
}
