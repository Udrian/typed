using System.Threading.Tasks;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data
{
    public abstract class SaveContext
    {
        // Properties
        internal bool ShouldSave { get; set; }

        // Constructors
        public abstract void Init(IResourceModel resourceModel, object param = null);
        
        // Functions
        public abstract Task SaveAction(Project project);
    }
}
