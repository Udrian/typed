using System.Collections.Generic;

namespace TypeD.Models.DTO
{
    internal class TyperModuleDTO
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public List<TyperVersionDTO> Versions { get; set; } = new List<TyperVersionDTO>();
    }

    internal class TyperVersionDTO
    {
        public string Version { get; set; }
        public string Zip { get; set; }
    }
}
