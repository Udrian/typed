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

        // Paths
        internal string ModulePath { get { return IsLocal ? Path.GetDirectoryName(Version.Replace(";local", "")) : Path.Combine(ModuleModel.ModuleCachePath, Name, Version); } }
        internal string ModuleDLLPath { get { return IsLocal ? Version.Replace(";local", "") : Path.Combine(ModulePath, $"{Name}.dll"); } }
        internal string ProductPath { get { return Path.Combine(ModulePath, "product"); } }

        // Loaded data
        internal Assembly Assembly { get; set; }
        public TypeInfo ModuleTypeInfo { get; set; }
        internal TypeDModuleInitializer TypeDModuleInitializer { get; set; }
        public ModuleProduct Product { get; internal set; }

        public bool IsTypeD { get
            {
                if(Assembly == null)
                    return false;

                var types = Assembly.GetTypes();
                var retType = types.FirstOrDefault(
                    t =>
                    {
                        return t.IsSubclassOf(typeof(TypeDModuleInitializer));
                    });
                return retType != null;
            }
        }

    }
}
