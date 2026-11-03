using System.Collections.Generic;

namespace TypeD.Models.Data
{
    /// <summary>
    /// A dependency of a TypeO project as resolved by typer.
    /// </summary>
    public class ModuleDependency
    {
        /// <summary>Gets or sets the dependency name.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the author (GitHub owner) of the dependency.</summary>
        public string Author { get; set; }
        /// <summary>Gets or sets the version declared in typeo.json.</summary>
        public string Version { get; set; }
        /// <summary>Gets or sets the tags of the dependency, such as dev or test.</summary>
        public List<string> Tags { get; set; } = new List<string>();
        /// <summary>Gets or sets a value indicating whether the dependency is a NuGet package.</summary>
        public bool Nuget { get; set; }
        /// <summary>Gets or sets a value indicating whether the dependency is overridden in typeo_override.json.</summary>
        public bool Overridden { get; set; }
        /// <summary>Gets or sets the path of the local module used, empty if the dependency is not local.</summary>
        public string LocalPath { get; set; } = "";
        /// <summary>Gets a value indicating whether the dependency resolves to a local module.</summary>
        public bool IsLocal { get { return !string.IsNullOrEmpty(LocalPath); } }
    }
}
