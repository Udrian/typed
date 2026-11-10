using TypeD.Models;
using TypeD.Models.Data;
using TypeD.Models.Data.PropertyDefinitions;
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
                new TyperModel(),
                new ProjectModel(),
                new HookModel(),
                new SaveModel(),
                new CrashModel(),
                new UINotifyModel(),
                new LogModel(),
                new GameLogModel(),
                new RestoreModel(),
                new ComponentModel(),
                new SettingModel(),
                new PropertyModel(),
                new FormatVersionModel(),
                new IconModel(),
                new PanelModel(),
                new CommandHistoryModel(),
                new NodeSelectionModel(),
                new ViewerCameraModel(),
                new NodeClipboardModel(),
                new EditorCommandRegistry(),
            // Providers
                new RecentProvider(),
                new ModuleProvider(),
                new ProjectProvider(),
                new ComponentProvider(),
                new ContentProvider()
            });
            var properties = resourceModel.Get<IPropertyModel>();
            properties.Register(new BooleanPropertyDefinition());
            properties.Register(new StringPropertyDefinition());
            properties.Register(new Int32PropertyDefinition());
            properties.Register(new DoublePropertyDefinition());
            properties.Register(new EnumPropertyDefinition());
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
            BrandingSettingContext.LoadWorkspace(project.Location);
            SettingModel.InitContext<BrandingSettingContext>(project);
            SettingModel.InitContext<ThemeSettingContext>(project);
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
            SettingModel.RemoveContext<BrandingSettingContext>();
            SettingModel.InitContext<ThemeSettingContext>(null);
        }
    }
}
