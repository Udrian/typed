using System.Collections.Generic;
using System.Linq;
using TypeD.Models.DTO;

namespace TypeD.Models.Data
{
    /// <summary>
    /// The product description of one module version: its version, external files, dependencies and optional dev module.
    /// </summary>
    public class ModuleTypeO
    {
        /// <summary>Gets or sets the module version.</summary>
        public string Version { get; set; }
        /// <summary>Gets or sets the external files that ship with the module.</summary>
        public List<string> Externals { get; set; }
        /// <summary>Gets or sets the names of the modules this module depends on.</summary>
        public List<string> Dependencies { get; set; }
        /// <summary>Gets or sets the name of the module's development (editor side) assembly, if any.</summary>
        public string DevModuleName { get; set; }

        internal ModuleTypeO()
        {
            Dependencies = new List<string>();
            Externals = new List<string>();
        }

        internal ModuleTypeO(ModuleListModuleDTO dto)
        {
            Dependencies = dto.Dependencies;
            Externals = dto.Externals;
            Version = dto.Version;
            DevModuleName = dto.Project?.FirstOrDefault(project => project.Type == "dev")?.Name;
        }
    }
}
