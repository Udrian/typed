namespace TypeD.Models.Data
{
    /// <summary>Values entered when defining a new component type.</summary>
    public class ComponentCreationOptions
    {
        /// <summary>Gets or sets the component class name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the component namespace.</summary>
        public string Namespace { get; set; }
        /// <summary>Gets or sets the inherited component.</summary>
        public Component ParentComponent { get; set; }
        /// <summary>Gets or sets the root component type name.</summary>
        public string BaseType { get; set; }
        /// <summary>Gets or sets the selected icon, or null to inherit it.</summary>
        public string Icon { get; set; }
        /// <summary>Gets or sets whether the component supports updates.</summary>
        public bool Updatable { get; set; }
        /// <summary>Gets or sets whether the component supports drawing.</summary>
        public bool Drawable { get; set; }
    }
}