using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Manages the icons used for component types. The icons live in the icons folder of each project and are seeded
    /// from a repository of default icons.
    /// </summary>
    public interface IIconModel : IModel
    {
        /// <summary>
        /// Gets or sets the folder containing the default icons that are copied into every project.
        /// </summary>
        public string DefaultIconsPath { get; set; }

        /// <summary>
        /// Copies the default icons that are missing from the icons folder of the project, without overwriting existing files.
        /// </summary>
        public void Sync(Project project);

        /// <summary>
        /// Lists the file names of all icons available in the project.
        /// </summary>
        public List<string> List(Project project);

        /// <summary>
        /// Gets the full path of an icon, falling back to the default icon if it doesn't exist. Returns <see langword="null"/> if neither exists.
        /// </summary>
        public string GetPath(Project project, string iconName);

        /// <summary>
        /// Copies an image from disk into the icons folder of the project and returns the file name to use as icon.
        /// </summary>
        public string Import(Project project, string sourceFilePath);
    }
}
