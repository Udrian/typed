namespace TypeD.Models.Data
{
    /// <summary>
    /// Provides the project, property, and value used to change a component property.
    /// </summary>
    public class ChangePropertyCommandData
    {
        /// <summary>
        /// The project containing the component.
        /// </summary>
        public Project Project { get; set; }
        /// <summary>
        /// The property whose value is being changed.
        /// </summary>
        public Property Property { get; set; }
        /// <summary>
        /// The component owning the property. Defaults to the selected node.
        /// </summary>
        public Component Component { get; set; }
        /// <summary>
        /// The new property value.
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Also applies the value to the same-named property of the other selected nodes.
        /// </summary>
        public bool ApplyToSelection { get; set; }
    }
}
