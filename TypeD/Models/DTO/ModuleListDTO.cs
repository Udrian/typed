using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TypeD.Models.DTO
{
    class ModuleListModuleDTO
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public List<string> Externals { get; set; }
        public List<string> Dependencies { get; set; }
        public string Type { get; set; }
        public List<ModuleListProjectDTO> Project { get; set; }
    }

    class ModuleListProjectDTO
    {
        public string Name { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("depends_on")]
        public List<string> DependsOn { get; set; }
    }

    class ModuleListDTO
    {
        public Dictionary<string, List<ModuleListModuleDTO>> Modules { get; set; }
    }
}
