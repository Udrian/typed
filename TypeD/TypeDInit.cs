using System.Collections.Generic;
using TypeD.Models;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers;

namespace TypeD
{
    public static class TypeDInit
    {
        public static void Init(IResourceModel resourceModel)
        {
            resourceModel.Add(new List<object>() {
            //Models
                new ModuleModel(),
                new ProjectModel(),
                new HookModel(),
                new SaveModel(),
                new UINotifyModel(),
                new LogModel(),
                new RestoreModel(),
                new ComponentModel(),
                new SettingModel(),
                new PanelModel(),
            // Providers
                new RecentProvider(),
                new ModuleProvider(),
                new ProjectProvider(),
                new ComponentProvider(),
            });
        }
    }
}
