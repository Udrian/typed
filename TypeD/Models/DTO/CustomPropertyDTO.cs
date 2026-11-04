namespace TypeD.Models.DTO
{
    /// <summary>A user defined property as stored in component files.</summary>
    public class CustomPropertyDTO
    {
        /// <summary>Gets or sets the property name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the description of the property.</summary>
        public string Description { get; set; }
        /// <summary>Gets or sets the full name of the property value type.</summary>
        public string Type { get; set; }
        /// <summary>Gets or sets the default value of the property.</summary>
        public object DefaultValue { get; set; }
        /// <summary>Gets or sets the editor group of the property.</summary>
        public string Group { get; set; }
        /// <summary>Gets or sets the access modifier of the property: Public, Private or Protected.</summary>
        public string Access { get; set; }
    }
}
