using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface ISaveModel : IModel
    {
        public bool AnythingToSave { get; }
        public void AddSave<T>(object param = null) where T : SaveContext, new();
        public void SaveNow<T>(Project project, object param = null) where T : SaveContext, new();
        public bool SaveContextExists<T>() where T : SaveContext, new();
        public T GetSaveContext<T>(object param = null) where T : SaveContext, new();
        public Task Save(Project project);
    }
}
