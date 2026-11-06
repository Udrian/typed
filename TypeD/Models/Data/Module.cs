using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using TypeD.Models.DTO;

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
        /// <summary>Gets or sets the module author (GitHub owner).</summary>
        public string Author { get; set; }
        /// <summary>Gets or sets the module version; a ";local" suffix marks a module loaded from a local path.</summary>
        public string Version { get; set; }
        /// <summary>Gets a value indicating whether the module is loaded from a local path instead of the module cache.</summary>
        public bool IsLocal { get { return Version.EndsWith(";local"); } }
        /// <summary>Gets a value indicating whether the module has a development (editor side) module.</summary>
        public bool HaveDevModule { get { return !string.IsNullOrEmpty(TypeO?.DevModuleName); } }

        // Paths
        // Modules are built from source, so the dll is located in the project's bin output instead of a hardcoded configuration and framework
        internal string ModulePath { get { return IsLocal ? Version.Replace(";local", "") : Path.Combine(ModuleModel.ModuleCachePath, Author, Name, Version); } }
        internal string ModuleDLLPath { get { return FindDLL(Name); } }
        internal string ModuleDevDLLPath { get { return HaveDevModule ? FindDLL(TypeO.DevModuleName) : ""; } }
        internal string TypeOPath { get { return Path.Combine(ModulePath, "typeo.json"); } }

        // Picks the newest framework compatible with the running editor, then the most recently built output
        private string FindDLL(string project)
        {
            var bin = Path.Combine(ModulePath, project, "bin");
            if (!Directory.Exists(bin)) return Path.Combine(bin, $"{project}.dll");

            var match = Directory.EnumerateDirectories(bin)
                .SelectMany(configuration => Directory.EnumerateDirectories(configuration))
                .Select(directory => new { Dll = Path.Combine(directory, $"{project}.dll"), Framework = ParseFramework(Path.GetFileName(directory)) })
                .Where(x => x.Framework != null && File.Exists(x.Dll))
                .OrderByDescending(x => x.Framework)
                .ThenByDescending(x => File.GetLastWriteTimeUtc(x.Dll))
                .FirstOrDefault();
            return match?.Dll ?? Path.Combine(bin, $"{project}.dll");
        }

        private static System.Version ParseFramework(string folder)
        {
            var m = Regex.Match(folder, @"^net(\d+)\.(\d+)(?:-([a-z]+)[\d.]*)?$");
            if (!m.Success) return null;

            var platform = m.Groups[3].Value;
            if (platform != "" && !(platform == "windows" && OperatingSystem.IsWindows())) return null;

            var version = new System.Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
            return version.Major <= Environment.Version.Major ? version : null;
        }

        // Loaded data
        internal Assembly Assembly { get; set; }
        internal Assembly DevAssembly { get; set; }
        // Whether the loaded assemblies have been initialized; assemblies loaded on demand are initialized by the next resolve
        internal bool Initialized { get; set; }
        /// <summary>Gets or sets the type info of the module's entry type in the loaded assembly.</summary>
        public TypeInfo ModuleTypeInfo { get; set; }
        internal TypeDModuleInitializer TypeDModuleInitializer { get; set; }
        internal List<IDisposable> FormatVersionRegistrations { get; } = new();
        /// <summary>Gets the product description (dependencies, externals, dev module name) of the module.</summary>
        public ModuleTypeO TypeO
        {
            get
            {
                if (typeO == null && File.Exists(TypeOPath))
                {
                    typeO = new ModuleTypeO(JsonSerializer.Deserialize<ModuleListModuleDTO>(File.ReadAllText(TypeOPath), TypeOJsonOptions));
                }
                return typeO;
            }
            internal set { typeO = value; }
        }
        private ModuleTypeO typeO;
        // Plain options, since the editor's converters registered by loaded modules must not affect how product files are read
        private static readonly JsonSerializerOptions TypeOJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }
}
