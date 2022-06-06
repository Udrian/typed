using System.Collections.Generic;

namespace TypeD.Models.DTO
{
    class ProjectDTO
    {
        public string ProjectName { get; set; }
        public string CSSolutionPath { get; set; }
        public string CSProjName { get; set; }
        public List<ModuleDTO> Modules { get; set; } = new List<ModuleDTO>();
        public string StartScene { get; set; }
    }
}
