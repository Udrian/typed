using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TypeD.Models.Data
{
    public class Module
    {
        // Data
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsLocal { get { return Version.EndsWith(";local"); } }
        public bool HaveDevModule { get { return !string.IsNullOrEmpty(Product.DevModuleName); } }

        // Paths
        internal string ModulePath { get { return IsLocal ? Version.Replace(";local", "") : Path.Combine(ModuleModel.ModuleCachePath, Name, Version); } }
        internal string ModuleDLLPath { get { return IsLocal ? Path.Combine(ModulePath, Name, "bin", "Debug", "net7.0", $"{Name}.dll") : Path.Combine(ModulePath, $"{Name}.dll"); } }
        internal string ModuleDevDLLPath { get { return HaveDevModule ? (IsLocal ? Path.Combine(ModulePath, Product.DevModuleName, "bin", "Debug", "net7.0-windows", $"{Product.DevModuleName}.dll") : Path.Combine(ModulePath, $"{Product.DevModuleName}.dll")) : ""; } }
        internal string ProductPath { get { return Path.Combine(ModulePath, "product"); } }

        // Loaded data
        internal Assembly Assembly { get; set; }
        internal Assembly DevAssembly { get; set; }
        public TypeInfo ModuleTypeInfo { get; set; }
        internal TypeDModuleInitializer TypeDModuleInitializer { get; set; }
        public ModuleProduct Product { get; internal set; }
    }
}
