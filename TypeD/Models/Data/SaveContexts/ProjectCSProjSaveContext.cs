using System.Threading.Tasks;
using System.Xml.Linq;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    /// <summary>
    /// Save context that writes the csproj file of a project.
    /// </summary>
    public class ProjectCSProjSaveContext : SaveContext
    {
        // Properties
        /// <summary>Gets or sets the path of the csproj file.</summary>
        public string Path { get; set; }
        /// <summary>Gets or sets the loaded csproj XML.</summary>
        public XElement CSProj { get; set; }

        // Constructors
        /// <inheritdoc/>
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            if(param is string)
            {
                Path = param as string;
                CSProj = XElement.Load(Path);
            }
        }

        // Functions
        /// <inheritdoc/>
        public override Task SaveAction(Project project)
        {
            return Task.Run(() =>
            {
                CSProj.Save(Path);
            });
        }

        /// <inheritdoc/>
        public override Task SaveAutosaveAction(Project project, Project snapshotProject)
        {
            return Task.Run(() =>
            {
                var sourcePath = System.IO.Path.GetFullPath(Path);
                var sourceRoot = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(project.Location));
                var relativePath = System.IO.Path.GetRelativePath(sourceRoot, sourcePath);
                if (System.IO.Path.IsPathRooted(relativePath) || relativePath == ".." ||
                    relativePath.StartsWith($"..{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                    throw new InvalidOperationException("The project file is outside the project folder.");

                var targetPath = System.IO.Path.Combine(snapshotProject.Location, relativePath);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetPath));
                CSProj.Save(targetPath);
            });
        }
    }
}
