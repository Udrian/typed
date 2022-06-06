using System.Collections.Generic;
using TypeD.Models.DTO;

namespace TypeD.Models.Data
{
    public class ModuleProduct
    {
        public string Version { get; set; }
        public List<string> Externals { get; set; }
        public List<string> Dependencies { get; set; }
        public bool TypeD { get; set; }

        internal ModuleProduct()
        {
            Dependencies = new List<string>();
            Externals = new List<string>();
        }

        internal ModuleProduct(ModuleListModuleDTO dto)
        {
            Dependencies = dto.Dependencies;
            Externals = dto.Externals;
            Version = dto.Version;
            TypeD = dto.Type == "TypeDModule";
        }
    }
}
