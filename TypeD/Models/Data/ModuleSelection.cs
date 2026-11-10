using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Computes the module versions a set of selected modules requires, so dependent versions can be
    /// updated to the required version and outdated selections can be warned about.
    /// </summary>
    public static class ModuleSelection
    {
        /// <summary>A module version required by a selected module.</summary>
        public class Requirement
        {
            /// <summary>Gets or sets the name of the required module.</summary>
            public string Name { get; set; }
            /// <summary>Gets or sets the author (GitHub owner) of the required module.</summary>
            public string Author { get; set; }
            /// <summary>Gets or sets the required version, the highest any selected module requires.</summary>
            public string Version { get; set; }
            /// <summary>Gets or sets the name of the selected module that requires the version.</summary>
            public string RequiredBy { get; set; }
        }

        /// <summary>Compares two version strings by their numeric parts, ignoring branch suffixes such as <c>.dev</c>.</summary>
        /// <param name="a">The first version.</param>
        /// <param name="b">The second version.</param>
        /// <returns>Negative when <paramref name="a"/> is lower, positive when higher, zero when equal.</returns>
        public static int CompareVersions(string a, string b)
        {
            var left = Parts(a);
            var right = Parts(b);
            for (var i = 0; i < Math.Max(left.Count, right.Count); i++)
            {
                var l = i < left.Count ? left[i] : 0;
                var r = i < right.Count ? right[i] : 0;
                if (l != r) return l.CompareTo(r);
            }
            return 0;
        }

        private static List<int> Parts(string version)
        {
            return Regex.Matches(version ?? "", @"\d+").Select(m => int.Parse(m.Value)).ToList();
        }

        /// <summary>Returns the highest plain release version, ignoring branch tags such as <c>1.0.0.feature</c>.</summary>
        /// <param name="versions">The versions to pick from.</param>
        /// <returns>The latest release version, or <c>null</c> if there is none.</returns>
        public static string LatestVersion(IEnumerable<string> versions)
        {
            return versions.Where(v => v != null && Regex.IsMatch(v, @"^\d+(\.\d+)*$"))
                .OrderByDescending(v => v, Comparer<string>.Create(CompareVersions)).FirstOrDefault();
        }

        /// <summary>
        /// The TypeO modules a product description depends on, excluding NuGet packages, the editor itself and
        /// test-only dependencies; dev-tagged module dependencies are included since they load into the editor.
        /// </summary>
        /// <param name="typeO">The product description of the module.</param>
        /// <returns>The module dependencies.</returns>
        public static IEnumerable<ModuleDependency> ModuleDependencies(ModuleTypeO typeO)
        {
            foreach (var dependencyString in typeO?.Dependencies ?? Enumerable.Empty<string>())
            {
                var dependency = ModuleDependency.Parse(dependencyString);
                if (dependency.Nuget || dependency.IsLocal || dependency.Name == "TypeD") continue;
                if (dependency.Tags.Any(tag => tag != "dev")) continue;
                yield return dependency;
            }
        }

        /// <summary>The highest version of each module required by the selected modules.</summary>
        /// <param name="selected">The selected modules with their product descriptions.</param>
        /// <returns>The required versions by module name.</returns>
        public static Dictionary<string, Requirement> RequiredVersions(IEnumerable<(string Name, ModuleTypeO TypeO)> selected)
        {
            var required = new Dictionary<string, Requirement>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, typeO) in selected)
            {
                foreach (var dependency in ModuleDependencies(typeO))
                {
                    if (!required.TryGetValue(dependency.Name, out var existing) || CompareVersions(dependency.Version, existing.Version) > 0)
                    {
                        required[dependency.Name] = new Requirement()
                        {
                            Name = dependency.Name,
                            Author = dependency.Author,
                            Version = dependency.Version,
                            RequiredBy = name
                        };
                    }
                }
            }
            return required;
        }

        /// <summary>
        /// The warning for a selected module, or <c>null</c>: either its version is lower than another selected
        /// module requires, or it requires a lower version of a dependency than the one selected and may need an update.
        /// </summary>
        /// <param name="name">The name of the selected module.</param>
        /// <param name="version">The selected version of the module.</param>
        /// <param name="typeO">The product description of the selected version, or <c>null</c> when unknown.</param>
        /// <param name="required">The required versions, from <see cref="RequiredVersions"/>.</param>
        /// <param name="selectedVersion">Looks up the selected version of a module by name, <c>null</c> when not selected.</param>
        /// <returns>The warning text, or <c>null</c> when the selection is consistent.</returns>
        public static string Warning(string name, string version, ModuleTypeO typeO, Dictionary<string, Requirement> required, Func<string, string> selectedVersion)
        {
            if (required.TryGetValue(name, out var requirement) && CompareVersions(version, requirement.Version) < 0)
            {
                return $"{name} needs to be updated to {requirement.Version}, required by {requirement.RequiredBy}";
            }
            foreach (var dependency in ModuleDependencies(typeO))
            {
                var selected = selectedVersion(dependency.Name);
                if (selected != null && CompareVersions(dependency.Version, selected) < 0)
                {
                    return $"{name} was made for {dependency.Name} {dependency.Version} but {selected} is selected, so {name} may need an update";
                }
            }
            return null;
        }
    }
}
