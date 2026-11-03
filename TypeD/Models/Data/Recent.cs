using System;

namespace TypeD.Models.Data
{
    /// <summary>
    /// An entry of the recently opened projects list.
    /// </summary>
    public class Recent
    {
        /// <summary>Gets or sets the project name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the path of the project.</summary>
        public string Path { get; set; }
        /// <summary>Gets or sets the time the project was last opened.</summary>
        public DateTime DateTime { get; set; }
    }
}
