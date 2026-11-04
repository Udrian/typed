using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace TypeD.Models
{
    // Loads a module assembly from memory so its file is not locked and the module can be rebuilt while the editor runs.
    // Every load gets a new context, since a context can hold only one version of an assembly.
    internal sealed class ModuleLoadContext : AssemblyLoadContext
    {
        private readonly string Folder;
        private readonly Func<AssemblyName, Assembly> FindModuleAssembly;

        public ModuleLoadContext(string name, string folder, Func<AssemblyName, Assembly> findModuleAssembly) : base(name)
        {
            Folder = folder;
            FindModuleAssembly = findModuleAssembly;
        }

        public Assembly LoadFile(string path)
        {
            using var assembly = new MemoryStream(File.ReadAllBytes(path));
            var pdbPath = Path.ChangeExtension(path, ".pdb");
            using var pdb = File.Exists(pdbPath) ? new MemoryStream(File.ReadAllBytes(pdbPath)) : null;
            return LoadFromStream(assembly, pdb);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Other modules resolve to their latest loaded version, and the editor's own assemblies are shared through the default context
            var module = FindModuleAssembly(assemblyName);
            if (module != null) return module;
            if (ModuleModel.IsEditorAssembly(assemblyName.Name)) return null;

            var path = Path.Combine(Folder, $"{assemblyName.Name}.dll");
            return File.Exists(path) ? LoadFile(path) : null;
        }
    }
}
