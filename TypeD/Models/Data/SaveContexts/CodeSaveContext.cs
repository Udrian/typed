using TypeD.Code;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    /// <summary>
    /// Save context that generates and saves code files.
    /// </summary>
    public class CodeSaveContext : SaveContext
    {
        // Properties
        /// <summary>Gets or sets the code generators whose output is saved.</summary>
        public List<Codalyzer> Codes { get; set; }

        // Constructors
        /// <inheritdoc/>
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            Codes = new List<Codalyzer>();
        }

        // Functions
        /// <inheritdoc/>
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
