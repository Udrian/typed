using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class SaveModel : ISaveModel
    {
        // Data
        private Dictionary<Type, SaveContext> SaveContexts { get; set; }

        // Models
        IResourceModel ResourceModel { get; set; }
        IUINotifyModel UINotifyModel { get; set; }

        // Constructors
        public SaveModel()
        {
            SaveContexts = new Dictionary<Type, SaveContext>();
        }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            UINotifyModel = ResourceModel.Get<IUINotifyModel>();
        }

        // Properties
        public bool AnythingToSave { get { return SaveContexts.Any(c => c.Value.ShouldSave); } }

        // Functions
        public void AddSave<T>(object param = null) where T : SaveContext, new()
        {
            var context = GetSaveContext<T>(param);
            if(!context.ShouldSave)
            {
                context.ShouldSave = true;

                UINotifyModel.Notify("AnythingToSave");
            }
        }

        public async void SaveNow<T>(Project project, object param = null) where T : SaveContext, new()
        {
            var context = GetSaveContext<T>(param);

            SaveContexts.Remove(context.GetType());
            await context.SaveAction(project);

        }

        public bool SaveContextExists<T>() where T : SaveContext, new()
        {
            return SaveContexts.ContainsKey(typeof(T));
        }

        public T GetSaveContext<T>(object param = null) where T : SaveContext, new()
        {
            if (!SaveContextExists<T>())
            {
                var newContext = new T();
                SaveContexts.Add(typeof(T), newContext);
                newContext.Init(ResourceModel, param);
            }
            return SaveContexts[typeof(T)] as T;
        }

        public async Task Save(Project project)
        {
            foreach(var saveContext in SaveContexts.Values)
            {
                if(saveContext.ShouldSave)
                    await saveContext.SaveAction(project);
            }
            SaveContexts.Clear();
            UINotifyModel.Notify("AnythingToSave");
        }
    }
}
