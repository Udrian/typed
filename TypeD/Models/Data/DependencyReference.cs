namespace TypeD.Models.Data
{
    /// <summary>
    /// Reference to a local or remote Dependency used for instance when creating a new local Module.
    /// </summary>
    public class DependencyReference
    {
        /// <summary>
        /// The dependency module author name.
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// Name of the dependency module.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// What version to depend on.
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Local path if applicable.
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// Reference to a local or remote Dependency used for instance when creating a new local Module.
        /// </summary>
        /// <param name="author">The dependency module author name.</param>
        /// <param name="name">Name of the dependency module.</param>
        /// <param name="version">What version to depend on.</param>
        /// <param name="localPath">Local path if applicable.</param>
        public DependencyReference(string author, string name, string version, string localPath = null)
        {
            Author = author;
            Name = name;
            Version = version;
            LocalPath = localPath;
        }
    }
}
