using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ModuleProvider : IModuleProvider
    {
        // Models
        ITyperModel TyperModel { get; set; }

        // Constructors
        public ModuleProvider() { }

        public void Init(IResourceModel resourceModel)
        {
            TyperModel = resourceModel.Get<ITyperModel>();
        }

        // Functions
        public async Task<IEnumerable<ModuleList>> List(Project project)
        {
            var moduleList = (await TyperModel.ListModules()).Where(m => m.Name != "TypeD").ToList();
            
            if(project == null)
                return moduleList;

            // Modules in use that typer does not know about, for example local ones, are listed with the version in use
            foreach (var module in project.Modules)
            {
                var existingModule = moduleList.Find(m => m.Name == module.Name);
                if (existingModule == null)
                {
                    moduleList.Add(new ModuleList() { Name = module.Name, Versions = new List<ModuleTypeO>() { new ModuleTypeO() { Version = module.Version } } });
                }
                else if (!module.IsLocal && !existingModule.Versions.Exists(v => v.Version == module.Version))
                {
                    existingModule.Versions.Add(new ModuleTypeO() { Version = module.Version });
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
