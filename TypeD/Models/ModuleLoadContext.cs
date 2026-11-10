using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace TypeD.Models
{
    // Loads a module assembly from memory so its file is not locked and the module can be rebuilt while the editor runs.
    // Every load gets a new context, since a context can hold only one version of an assembly.
    internal sealed class ModuleLoadContext : AssemblyLoadContext
    {
        private static readonly object SharedDependencyLock = new();
        private static readonly Dictionary<string, Assembly> SharedDependencies = new(StringComparer.OrdinalIgnoreCase);
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
            // A module assembly that could not be loaded must not be replaced by a copy from this module's bin folder
            if (ModuleModel.IsKnownModuleAssembly(assemblyName.Name)) return null;

            lock (SharedDependencyLock)
            {
                // Shared by simple name, so every module and every requested version binds the same instance;
                // loading the same file once per requested version would split its static state (e.g. GL bindings)
                if (SharedDependencies.TryGetValue(assemblyName.Name, out var dependency))
                    return dependency;

                var path = Path.Combine(Folder, $"{assemblyName.Name}.dll");
                if (!File.Exists(path))
                    return null;

                dependency = LoadFile(path);
                SharedDependencies.Add(assemblyName.Name, dependency);
                return dependency;
            }
        }
    }
}
