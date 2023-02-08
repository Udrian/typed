using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class ModuleModel : IModuleModel
    {
        // Models
        public IHookModel HookModel { get; set; }
        public IResourceModel ResourceModel { get; set; }
        public ISaveModel SaveModel { get; set; }

        // Paths
        public static string ModuleCachePath { get { return $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/TypeO/ModulesCache"; } }

        // Constructors
        public ModuleModel() { }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            HookModel = ResourceModel.Get<IHookModel>();
            SaveModel = ResourceModel.Get<ISaveModel>();
        }

        // Functions
        public async Task<bool> Download(Module module, Action<long, int, long> progress)
        {
            if (module.Version.EndsWith("local")) return false;
            if (Directory.Exists($"{module.ModulePath}")) return false;

            Directory.CreateDirectory(module.ModulePath);

            var zipName = $"{module.Name}-{module.Version}.zip";
            var moduleUrl = new Uri($"https://typedeaf.nyc3.cdn.digitaloceanspaces.com/typeo/releases/modules/{module.Name}/{module.Version}/{zipName}");
            var downloadZipPath = $"{module.ModulePath}/{zipName}";

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (s, e) => {
                    progress(e.BytesReceived, e.ProgressPercentage, e.TotalBytesToReceive);
                };
                await client.DownloadFileTaskAsync(moduleUrl, downloadZipPath);
            }
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(downloadZipPath, module.ModulePath);
            });

            await Task.Run(() =>
            {
                File.Delete(downloadZipPath);
            });

            return true;
        }

        public void LoadAssembly(Project project, Module module)
        {
            if (!File.Exists(module.ModuleDLLPath)) return;
            if(File.Exists(module.ProductPath))
            {
                var dto = JSON.Deserialize<ModuleListModuleDTO>(module.ProductPath, true);
                module.Product = new ModuleProduct(dto);
            }

            module.Assembly = System.Reflection.Assembly.LoadFrom(module.ModuleDLLPath);
            module.ModuleTypeInfo = GetModuleType(module);
            InitializeTypeD(project, module);
        }

        public void UnloadAssembly(Module module)
        {
            if (module.Assembly == null) return;

            UninitializeTypeD(module);
            module.Assembly = null;
            module.ModuleTypeInfo = null;
        }

        public void AddToProjectXML(Module module, XElement project)
        {
            AddReference(module, GetItemGroup(project, "Debug"));
            AddReference(module, GetItemGroup(project, "Release"));
        }

        public void RemoveFromProjectXML(Module module, XElement project)
        {
            RemoveReference(module, GetItemGroup(project, "Debug"));
            RemoveReference(module, GetItemGroup(project, "Release"));
        }

        // Internal functions
        private void InitializeTypeD(Project project, Module module)
        {
            if (!module.IsTypeD) return;

            var typeDInitType = module.Assembly.GetTypes().FirstOrDefault(t => { return t.IsSubclassOf(typeof(TypeDModuleInitializer)); });
            if (typeDInitType == null) return;

            module.TypeDModuleInitializer = Activator.CreateInstance(typeDInitType) as TypeDModuleInitializer;
            module.TypeDModuleInitializer.Hooks = HookModel;
            module.TypeDModuleInitializer.Resources = ResourceModel;
            module.TypeDModuleInitializer.Initializer(project);
        }

        private void UninitializeTypeD(Module module)
        {
            if (!module.IsTypeD) return;

            var typeDInitType = module.Assembly.GetTypes().FirstOrDefault(t => { return t.IsSubclassOf(typeof(TypeDModuleInitializer)); });
            if (typeDInitType == null) return;

            module.TypeDModuleInitializer.Uninitializer();
        }

        private System.Reflection.TypeInfo GetModuleType(Module module)
        {
            if (module.Name == "TypeOCore") return null;
            System.Reflection.TypeInfo moduleType = null;
            foreach (var type in module.Assembly.DefinedTypes)
            {
                if (type?.BaseType?.FullName == "TypeOEngine.Typedeaf.Core.Engine.Module")
                {
                    moduleType = type;
                    break;
                }
            }

            return moduleType;
        }

        private XElement GetItemGroup(XElement project, string configuration)
        {
            var configString = $"'$(Configuration)' == '{configuration}'";
            var itemGroups = project.Elements("ItemGroup");
            XElement itemGroup = null;

            foreach (var item in itemGroups)
            {
                if (item.Attribute("Condition")?.Value == configString)
                {
                    itemGroup = item;
                }
            }
            if (itemGroup == null)
            {
                itemGroup = new XElement("ItemGroup");
                itemGroup.Add(new XAttribute("Condition", configString));
                project.Add(itemGroup);
            }
            return itemGroup;
        }

        private void AddReference(Module module, XElement itemGroup)
        {
            var foundModule = false;
            foreach (var referenceElement in itemGroup.Elements("Reference"))
            {
                if (referenceElement.Attribute("Include")?.Value == module.Name)
                {
                    foundModule = true;
                    break;
                }
            }
            if (!foundModule)
            {
                var referenceElement = new XElement("Reference");
                referenceElement.Add(new XAttribute("Include", module.Name));

                referenceElement.Add(new XElement("HintPath", module.ModuleDLLPath));

                itemGroup.Add(referenceElement);
            }
        }

        private void RemoveReference(Module module, XElement itemGroup)
        {
            foreach (var referenceElement in itemGroup.Elements("Reference"))
            {
                if (referenceElement.Attribute("Include")?.Value == module.Name)
                {
                    referenceElement.Remove();
                    break;
                }
            }
        }
    }
}
