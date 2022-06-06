using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class RestoreModel : IRestoreModel
    {
        // Properties
        private List<Action<Project>> RestoreMethods { get; set; }

        // Constructors
        public RestoreModel()
        {
            RestoreMethods = new List<Action<Project>>();
        }

        public void Init(IResourceModel resourceModel)
        {
        }

        // Functions
        public void AddRestoreMethod(Action<Project> restore)
        {
            RestoreMethods.Add(restore);
        }

        public async Task Restore(Project project)
        {
            foreach(var restore in RestoreMethods)
            {
                await Task.Run(() =>
                {
                    restore(project);
                });
            }
        }
    }
}
