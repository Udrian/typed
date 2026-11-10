using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Parses a dependency string, for example <c>Author@Name-Version;tag</c>; the first '-' separates name and version.</summary>
        /// <param name="dependencyString">The dependency string.</param>
        /// <returns>The parsed dependency.</returns>
        public static ModuleDependency Parse(string dependencyString)
        {
            var parts = dependencyString.Split(';');
            var head = parts[0];
            var separator = head.IndexOf('-');
            var nameAuthor = separator >= 0 ? head[..separator] : head;
            var at = nameAuthor.IndexOf('@');
            var parameters = parts.Skip(1).ToList();

            var dependency = new ModuleDependency()
            {
                Author = at >= 0 ? nameAuthor[..at] : "",
                Name = at >= 0 ? nameAuthor[(at + 1)..] : nameAuthor,
                Version = separator >= 0 ? head[(separator + 1)..] : "",
                Nuget = parameters.Contains("nuget"),
                Tags = parameters.Where(p => p != "nuget" && p != "local").ToList()
            };
            if (parameters.Contains("local")) dependency.LocalPath = dependency.Version;
            return dependency;
        }
    }
}
