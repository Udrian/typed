using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TypeD.Models.Data
{
    /// <summary>
    /// A module (engine extension package) used by a project, identified by name and version.
    /// </summary>
    public class Module
    {
        // Data
        /// <summary>Gets or sets the module name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the module version; a ";local" suffix marks a module loaded from a local path.</summary>
        public string Version { get; set; }
        /// <summary>Gets a value indicating whether the module is loaded from a local path instead of the module cache.</summary>
        public bool IsLocal { get { return Version.EndsWith(";local"); } }
        /// <summary>Gets a value indicating whether the module has a development (editor side) module.</summary>
        public bool HaveDevModule { get { return !string.IsNullOrEmpty(Product.DevModuleName); } }

        // Paths
        internal string ModulePath { get { return IsLocal ? Version.Replace(";local", "") : Path.Combine(ModuleModel.ModuleCachePath, Name, Version); } }
        internal string ModuleDLLPath { get { return IsLocal ? Path.Combine(ModulePath, Name, "bin", "Debug", "net7.0", $"{Name}.dll") : Path.Combine(ModulePath, $"{Name}.dll"); } }
        internal string ModuleDevDLLPath { get { return HaveDevModule ? (IsLocal ? Path.Combine(ModulePath, Product.DevModuleName, "bin", "Debug", "net7.0-windows", $"{Product.DevModuleName}.dll") : Path.Combine(ModulePath, $"{Product.DevModuleName}.dll")) : ""; } }
        internal string ProductPath { get { return Path.Combine(ModulePath, "product"); } }

        // Loaded data
        internal Assembly Assembly { get; set; }
        internal Assembly DevAssembly { get; set; }
        /// <summary>Gets or sets the type info of the module's entry type in the loaded assembly.</summary>
        public TypeInfo ModuleTypeInfo { get; set; }
        internal TypeDModuleInitializer TypeDModuleInitializer { get; set; }
        /// <summary>Gets the product description (dependencies, externals, dev module name) of the module.</summary>
        public ModuleProduct Product { get; internal set; }
    }
}
