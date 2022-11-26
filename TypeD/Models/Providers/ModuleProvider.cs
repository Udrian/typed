using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ModuleProvider : IModuleProvider
    {
        // Constructors
        public ModuleProvider() { }

        public void Init(IResourceModel resourceModel) { }

        // Functions
        public async Task<IEnumerable<ModuleList>> List(Project project)
        {
            List<ModuleList> moduleList = new List<ModuleList>();

            var data = await APICaller.GetJsonObject<ModuleListDTO>("module/list");
            if(data?.Modules != null)
            {
                moduleList.AddRange(data.Modules.Select(m =>
                {
                    return new ModuleList()
                    {
                        Name = m.Key,
                        Versions = m.Value.Select(p =>
                        {
                            return new ModuleProduct(p);
                        }).ToList()
                    };
                }).ToList());
            }

            foreach (var module in project.Modules)
            {
                var existingModule = moduleList.Find(m => m.Name == module.Name);
                if (existingModule == null)
                {
                    moduleList.Add(new ModuleList() { Name = module.Name, Versions = new List<ModuleProduct>() { new ModuleProduct() { Version = module.Version } } });
                }
                else
                {
                    if(!existingModule.Versions.Exists(v => v.Version == module.Version))
                    {
                        existingModule.Versions.Add(new ModuleProduct() { Version = module.Version });
                    }
                }
            }

            return moduleList;
        }

        public Module Create(string name, string version)
        {
            var module = new Module() { Name = name, Version = version };

            return module;
        }
    }
}
