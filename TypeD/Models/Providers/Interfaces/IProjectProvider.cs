using System;
using System.Threading.Tasks;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IProjectProvider : IProvider
    {
        public Task<Project> Create(string projectName, string location, string csSolutionPath, string csProjName, Action<int> progress);
        public Task<Project> Load(string projectFilePath, Action<int> progress);
        public Task Save(Project project);
    }
}
