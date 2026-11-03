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
    }
}
