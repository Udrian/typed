namespace TypeD.Models.DTO
{
    /// <summary>A property value retained when a component change makes it incompatible with the current property.</summary>
    public class PreservedPropertyDTO
    {
        /// <summary>Gets or sets the original property name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the original value type, when known.</summary>
        public string TypeName { get; set; }
        /// <summary>Gets or sets the original declaring type, when known.</summary>
        public string DeclaringTypeName { get; set; }
        /// <summary>Gets or sets the original serialized value.</summary>
        public object Value { get; set; }
    }
}
