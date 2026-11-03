using System.Collections.Generic;

namespace TypeD.Models.Data
{
    /// <summary>
    /// A module with all its available versions.
    /// </summary>
    public class ModuleList
    {
        /// <summary>Gets or sets the module name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the available versions of the module.</summary>
        public List<ModuleTypeO> Versions { get; set; }
    }
}
