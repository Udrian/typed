namespace TypeD.Models.DTO
{
    /// <summary>A property name and value pair as stored in component files.</summary>
    public class PropertyDTO
    {
        /// <summary>Gets or sets the property name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the property value.</summary>
        public object Value { get; set; }
    }
}