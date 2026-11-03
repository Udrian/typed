using TypeD.Models;
using TypeD.Models.Data;
using TypeD.Models.Data.SettingContexts;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers;
using TypeD.ViewModel;

namespace TypeD
{
    /// <summary>
    /// Registers the TypeD models and providers.
    /// </summary>
    public static class TypeDInit
    {
        /// <summary>Registers all TypeD models and providers with the resource model.</summary>
        /// <param name="resourceModel">The resource model to register with.</param>
        public static void Init(IResourceModel resourceModel)
        {
            ViewModelBase.ResourceModel = resourceModel;

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
                new PropertyModel(),
                new IconModel(),
                new PanelModel(),
                new EditorCommandRegistry(),
            // Providers
                new RecentProvider(),
                new ModuleProvider(),
                new ProjectProvider(),
                new ComponentProvider(),
                new ContentProvider()
            });
        }

        /// <summary>Starts watching a loaded project and initializes its settings.</summary>
        /// <param name="project">The loaded project.</param>
        /// <param name="resourceModel">The resource model.</param>
        public static void ProjectLoad(Project project, IResourceModel resourceModel)
        {
            resourceModel.Get<IProjectModel>().StartWatching(project);

            // Settings
            var SettingModel = resourceModel.Get<ISettingModel>();
            SettingModel.InitContext<MainWindowSettingContext>(project);
        }

        /// <summary>Stops watching an unloaded project and removes its settings.</summary>
        /// <param name="project">The unloaded project.</param>
        /// <param name="resourceModel">The resource model.</param>
        public static void ProjectUnload(Project project, IResourceModel resourceModel)
        {
            resourceModel.Get<IProjectModel>().StopWatching(project);

            // Settings
            var SettingModel = resourceModel.Get<ISettingModel>();
            SettingModel.RemoveContext<MainWindowSettingContext>();
        }
    }
}
