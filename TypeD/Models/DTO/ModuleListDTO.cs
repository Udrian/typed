using System.Collections.Generic;

namespace TypeD.Models.DTO
{
    class ModuleListModuleDTO
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public List<string> Externals { get; set; }
        public List<string> Dependencies { get; set; }
        public string Type { get; set; }
    }

    class ModuleListDTO
    {
        public Dictionary<string, List<ModuleListModuleDTO>> Modules { get; set; }
    }
}
