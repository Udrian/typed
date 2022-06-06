using System.Threading.Tasks;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    public class ProjectSaveContext : SaveContext
    {
        // Providers
        IProjectProvider ProjectProvider { get; set; }

        // Properties
        public Project Project { get; set; }

        // Constructors
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            ProjectProvider = resourceModel.Get<IProjectProvider>();
            if (param is Project)
                Project = param as Project;
        }

        // Functions
        public override Task SaveAction(Project project)
        {
            return ProjectProvider.Save(Project);
        }
    }
}
