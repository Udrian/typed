using System.Threading.Tasks;
using System.Xml.Linq;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    public class ProjectCSProjSaveContext : SaveContext
    {
        // Properties
        public string Path { get; set; }
        public XElement CSProj { get; set; }

        // Constructors
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            if(param is string)
            {
                Path = param as string;
                CSProj = XElement.Load(Path);
            }
        }

        // Functions
        public override Task SaveAction(Project project)
        {
            return Task.Run(() =>
            {
                CSProj.Save(Path);
            });
        }
    }
}
