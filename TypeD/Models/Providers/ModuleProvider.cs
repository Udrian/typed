using System.Text.Json;
using TypeD.Models.Data;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ModuleProvider : IModuleProvider
    {
        // Models
        ITyperModel TyperModel { get; set; }
        ISettingModel SettingModel { get; set; }

        // Constructors
        public ModuleProvider() { }

        public void Init(IResourceModel resourceModel)
        {
            TyperModel = resourceModel.Get<ITyperModel>();
            SettingModel = resourceModel.Get<ISettingModel>();
        }

        // Functions
        public async Task<IEnumerable<ModuleList>> List(Project project)
        {
            // This might be called before a project have been loaded and initialized, which is why we initialize the setting if it's needed.
            var moduleSettingContext = SettingModel.GetContext<ModuleSettingContext>(SettingLevel.System);
            if (moduleSettingContext == null)
            {
                SettingModel.InitContext<ModuleSettingContext>(project);
                moduleSettingContext = SettingModel.GetContext<ModuleSettingContext>(SettingLevel.System);
            }

            var moduleList = (await TyperModel.ListModules(moduleSettingContext.Repositories.Value)).Where(m => m.Name != "TypeD").ToList();
            
            if(project == null)
                return moduleList;


            // Filter module versions based on semantic versioning or allowed branches
            moduleList.ForEach(m => m.Versions = m.Versions.Where(v => v.Version.Split('.').Length == 3 || moduleSettingContext.Branches.Value.Any(b => v.Version.EndsWith(b))).ToList());

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

        public Task<ModuleTypeO> Info(string author, string name, string version)
        {
            return TyperModel.GetModuleTypeO(author, name, version);
        }

        public ModuleList ReadLocal(string path)
        {
            var typeOPath = Path.Combine(path, "typeo.json");
            if (!File.Exists(typeOPath)) return null;

            try
            {
                var dto = JsonSerializer.Deserialize<ModuleListModuleDTO>(File.ReadAllText(typeOPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (string.IsNullOrEmpty(dto?.Name) || dto.Type != "module") return null;

                return new ModuleList()
                {
                    Name = dto.Name,
                    Author = "Local",
                    Versions = new List<ModuleTypeO>() { new ModuleTypeO(dto) }
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
