using System.Threading.Tasks;
using TypeD.Code;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface IProjectModel : IModel
    {
        public void AddModule(Project project, Module module);
        public void RemoveModule(Project project, string moduleName);
        public Task<bool> Build(Project project);
        public void Run(Project project);
        public void InitCode(Project project, Codalyzer code);
        public void SaveCode(Codalyzer code);
        public void InitAndSaveCode(Project project, Codalyzer code);
        public void SetStartScene(Project project, Component scene);
        public void BuildComponentTree(Project project);
        public bool LoadAssembly(Project project);
        public string TransformNamespaceString(Project project, string @namespace);
    }
}
