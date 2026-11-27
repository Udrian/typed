using System.ComponentModel;
using TypeD.Helpers;
using TypeD.Models.Data.Hooks;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    /// <summary>
    /// Save context that writes component definition files and removes the files of deleted or renamed components.
    /// </summary>
    public class ComponentSaveContext : SaveContext
    {
        // Models
        IProjectModel ProjectModel { get; set; }
        IHookModel HookModel { get; set; }
        IFormatVersionModel FormatVersionModel { get; set; }

        // Providers
        IComponentProvider ComponentProvider { get; set; }

        // Properties
        /// <summary>Gets or sets the project the components belong to.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the components to save.</summary>
        public List<Component> Components { get; set; }
        /// <summary>Gets or sets the components whose files are deleted.</summary>
        public List<Component> DeletedComponents { get; set; }
        /// <summary>Gets or sets the renamed components, each with its previous class name.</summary>
        public List<Tuple<string, Component>> RenamedComponents { get; set; }

        // Constructors
        /// <inheritdoc/>
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            Components = new List<Component>();
            DeletedComponents = new List<Component>();
            RenamedComponents = new List<Tuple<string, Component>>();
            if (param is Project)
                Project = param as Project;

            ProjectModel = resourceModel.Get<IProjectModel>();
            HookModel = resourceModel.Get<IHookModel>();
            FormatVersionModel = resourceModel.Get<IFormatVersionModel>();
            ComponentProvider = resourceModel.Get<IComponentProvider>();
        }

        // Functions
        /// <inheritdoc/>
        public override Task SaveAction(Project project)
        {
            return SaveActionToProject(project ?? Project);
        }

        /// <inheritdoc/>
        public override Task SaveAutosaveAction(Project project, Project snapshotProject)
        {
            return SaveActionToProject(snapshotProject);
        }

        private Task SaveActionToProject(Project targetProject)
        {
            return Task.Run(() => {
                foreach (var saveComponent in Components)
                {
                    JSON.Serialize(CreateDocument(saveComponent), ComponentProvider.GetPath(targetProject, saveComponent));
                }

                foreach (var deletedComponent in DeletedComponents)
                {
                    if (ComponentProvider.Exists(targetProject, deletedComponent))
                    {
                        var componentFile = ComponentProvider.GetPath(targetProject, deletedComponent);
                        File.Delete(componentFile);
                        var csFile = Path.Combine(targetProject.Location, $"{deletedComponent.FullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                        var csTypeDFile = Path.Combine(targetProject.ProjectComponentCodePath, $"{deletedComponent.FullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
                        if (File.Exists(csFile))
                            File.Delete(csFile);
                        if (File.Exists(csTypeDFile))
                            File.Delete(csTypeDFile);

                        if(!Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(componentFile)).Any())
                            Directory.Delete(Path.GetDirectoryName(componentFile));
                        if (!Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(csFile)).Any())
                            Directory.Delete(Path.GetDirectoryName(csFile));
                        if (!Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(csTypeDFile)).Any())
                            Directory.Delete(Path.GetDirectoryName(csTypeDFile));
                    }
                }

                foreach (var renamedComponent in RenamedComponents)
                {
                    var oldFullName = $"{renamedComponent.Item2.Namespace}.{renamedComponent.Item1}";

                    File.Delete(ComponentProvider.GetPath(targetProject, oldFullName));
                    var csFile = Path.Combine(targetProject.Location, $"{oldFullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                    var csTypeDFile = Path.Combine(targetProject.ProjectComponentCodePath, $"{oldFullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
                    var csFileNew = Path.Combine(targetProject.Location, $"{renamedComponent.Item2.FullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                    var csTypeDFileNew = Path.Combine(targetProject.ProjectComponentCodePath, $"{renamedComponent.Item2.FullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
                    if (File.Exists(csFile))
                    {
                        File.Move(csFile, csFileNew);
                        File.WriteAllText(csFileNew, File.ReadAllText(csFileNew).Replace($"class {renamedComponent.Item1}", $"class {renamedComponent.Item2.ClassName}"));
                    }
                    if (File.Exists(csTypeDFile))
                    {
                        File.Move(csTypeDFile, csTypeDFileNew);
                        File.WriteAllText(csTypeDFileNew, File.ReadAllText(csTypeDFileNew).Replace($"class {renamedComponent.Item1}", $"class {renamedComponent.Item2.ClassName}"));
                    }
                }

                if (!targetProject.IsClosing)
                    ProjectModel.BuildComponentTree(targetProject);
            });
        }

        /// <summary>Creates the persisted representation of a component without writing a file.</summary>
        /// <param name="component">The component to serialize.</param>
        /// <returns>The component document.</returns>
        public ComponentDTO CreateDocument(Component component)
        {
            ArgumentNullException.ThrowIfNull(component);
            component.ValidateUnresolvedPropertyOverrides();
            return new ComponentDTO
            {
                FormatVersions = FormatVersionModel.GetCurrentVersions(FormatDocumentKind.Component, component.FormatVersions),
                ExtensionData = component.ExtensionData,
                ClassName = component.ClassName,
                Interfaces = component.Interfaces.Select(i => i.FullName).ToList(),
                Namespace = component.Namespace,
                BaseInheritedComponent = component.BaseInheritedComponent?.FullName ?? "",
                TemplateClass = component.Template.GetType().FullName,
                Icon = component.Icon,
                Children = component.Children.Select(CreateChildDTO).ToList(),
                Properties = component.Properties
                    .Where(p => p.Name != "ID" && (component.HasPropertyOverridesFor(p.Name)
                        || component.PersistedPropertyOverrides.Contains(p.Name)))
                    .Select(p => new PropertyDTO { Name = p.Name, Value = p.Value })
                    .Concat(component.UnresolvedPropertyOverrides.Where(saved =>
                        component.Properties.All(property => property.Name != saved.Name))).ToList(),
                PreservedProperties = component.PreservedPropertyOverrides.Count == 0
                    ? null : component.PreservedPropertyOverrides.ToList(),
                CustomProperties = component.CustomProperties
                    .Select(p => new CustomPropertyDTO
                    {
                        Name = p.Name,
                        Description = p.Description,
                        Type = p.TypeName,
                        DefaultValue = p.DefaultValue,
                        Group = p.Group,
                        Access = p.Access.ToString()
                    }).ToList(),
                RemovedCustomProperties = component.RemovedCustomProperties.Count == 0
                    ? null : component.RemovedCustomProperties.ToList()
            };
        }

        private ComponentChildDTO CreateChildDTO(Component component)
        {
            component.ValidateUnresolvedPropertyOverrides();
            return new ComponentChildDTO
            {
                ID = component.ID,
                FullName = component.FullName,
                Removed = component.Removed,
                Public = component.Public,
                ChildOrder = component.ChildOrderChanged ? component.Children.Select(child => child.ID).ToList() : null,
                Properties = component.Properties
                    .Where(p => component.HasPropertyOverridesFor(p.Name))
                    .Select(p => new PropertyDTO
                    {
                        Name = p.Name,
                        Value = p.Value
                    }).Concat(component.UnresolvedPropertyOverrides.Where(saved =>
                        component.Properties.All(property => property.Name != saved.Name))).ToList(),
                PreservedProperties = component.PreservedPropertyOverrides.Count == 0
                    ? null : component.PreservedPropertyOverrides.ToList(),
                Children = component.Children
                    .Where(child => !component.DefinitionChildren.Contains(child) || child.HasChanges)
                    .Select(CreateChildDTO)
                    .ToList()
            };
        }
    }
}
