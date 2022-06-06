using System.Collections.Generic;
using System.Threading.Tasks;
using TypeD.Helpers;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    public class SettingSaveContext : SaveContext
    {
        // Properties
        public List<SettingContext> Settings { get; set; }

        // Models
        ISettingModel SettingModel { get; set; }

        // Constructors
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            Settings = new List<SettingContext>();

            SettingModel = resourceModel.Get<SettingModel>();
        }

        // Functions
        public override Task SaveAction(Project project)
        {
            return Task.Run(() =>
            {
                foreach (var setting in Settings)
                {
                    if (!setting.ShouldSave) continue;

                    var paths = new List<string>() { SettingModel.GetPath(project, SettingLevel.System), SettingModel.GetPath(project, SettingLevel.Workspace), SettingModel.GetPath(project, SettingLevel.Local) };
                    var path = paths[(int)setting.Level];
                    var filePath = $"{path}/{SettingModel.GetName(setting.GetType())}.json";
                    JSON.Serialize(setting, filePath);
                    setting.ShouldSave = false;
                }
            });
        }
    }
}
