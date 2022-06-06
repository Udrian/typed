using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface IModuleModel : IModel
    {
        public Task<bool> Download(Module module, Action<long, int, long> progress);
        public void LoadAssembly(Project project, Module module);
        public void UnloadAssembly(Module module);
        public void AddToProjectXML(Module module, XElement project);
        public void RemoveFromProjectXML(Module module, XElement project);
    }
}
