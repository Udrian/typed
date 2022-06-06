using System.Collections.Generic;

namespace TypeD.Models.Data
{
    public class ModuleList
    {
        public string Name { get; set; }
        public List<ModuleProduct> Versions { get; set; }
    }
}
