using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeD.Helpers;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    /// <summary>
    /// Save context that writes modified settings to their setting files.
    /// </summary>
    public class SettingSaveContext : SaveContext
    {
        // Properties
        /// <summary>Gets or sets the settings to save; only those marked as changed are written.</summary>
        public List<SettingContext> Settings { get; set; }

        // Models
        ISettingModel SettingModel { get; set; }

        // Constructors
        /// <inheritdoc/>
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            Settings = new List<SettingContext>();

            SettingModel = resourceModel.Get<SettingModel>();
        }

        // Functions
        /// <inheritdoc/>
        public override Task SaveAction(Project project)
        {
            return Task.Run(() =>
            {
                foreach (var setting in Settings)
                {
                    if (!setting.ShouldSave) continue;

                    var path = SettingModel.GetPath(project, setting.Level);
                    var filePath = $"{path}/{SettingModel.GetName(setting.GetType())}.json";
                    var document = JsonSerializer.SerializeToNode(setting, setting.GetType(), JSON.CreateOptions()).AsObject();
                    if (setting.Overrides != null)
                    {
                        foreach (var property in setting.GetType().GetProperties()
                            .Where(property => typeof(ISetting).IsAssignableFrom(property.PropertyType)))
                            if (!setting.Overrides.Contains(property.Name)) document.Remove(property.Name);
                    }
                    JSON.Serialize(document, filePath);
                    setting.ShouldSave = false;
                }
            });
        }
    }
}
