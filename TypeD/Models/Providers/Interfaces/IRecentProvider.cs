using System.Collections.Generic;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IRecentProvider : IProvider
    {
        public void Add(string projectFilePath, string projectName);
        public IEnumerable<Recent> Get();
    }
}
