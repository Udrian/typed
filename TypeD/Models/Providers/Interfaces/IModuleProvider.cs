using System.Collections.Generic;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IModuleProvider : IProvider
    {
        public Task<IEnumerable<ModuleList>> List(Project project);
        public Module Create(string name, string version);
    }
}
