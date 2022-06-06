using System.Collections.Generic;
using System.Threading.Tasks;
using TypeD.Code;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    public class CodeSaveContext : SaveContext
    {
        // Properties
        public List<Codalyzer> Codes { get; set; }

        // Constructors
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            Codes = new List<Codalyzer>();
        }

        // Functions
        public override Task SaveAction(Project project)
        {
            return Task.Run(() =>
            {
                foreach (var code in Codes)
                {
                    code.Generate();
                    code.Save();
                }
            });
        }
    }
}
