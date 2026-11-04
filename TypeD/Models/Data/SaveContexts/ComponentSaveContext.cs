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
            ComponentProvider = resourceModel.Get<IComponentProvider>();
        }

        // Functions
        /// <inheritdoc/>
        public override Task SaveAction(Project project)
        {
            return Task.Run(() => {
                foreach (var saveComponent in Components)
                {

                    JSON.Serialize(new ComponentDTO()
                    {
                        ClassName = saveComponent.ClassName,
                        Interfaces = saveComponent.Interfaces.Select(i => i.FullName).ToList(),
                        Namespace = saveComponent.Namespace,
                        BaseInheritedComponent = saveComponent.BaseInheritedComponent?.FullName ?? "",
                        TemplateClass = saveComponent.Template.GetType().FullName,
                        Icon = saveComponent.Icon,
                        Children = saveComponent.Children.Select(CreateChildDTO).ToList(),
                        Properties = saveComponent.Properties
                            .Where(p => p.Name != "ID" && saveComponent.HasPropertyOverridesFor(p.Name))
                            .Select(p => new PropertyDTO
                            {
                                Name = p.Name,
                                Value = p.Value
                            }).ToList(),
                        CustomProperties = saveComponent.CustomProperties
                            .Select(p => new CustomPropertyDTO
                            {
                                Name = p.Name,
                                Description = p.Description,
                                Type = p.TypeName,
                                DefaultValue = p.DefaultValue,
                                Group = p.Group,
                                Access = p.Access.ToString()
                            }).ToList()
                    }, ComponentProvider.GetPath(Project, saveComponent));
                }

                foreach (var deletedComponent in DeletedComponents)
                {
                    if (ComponentProvider.Exists(Project, deletedComponent))
                    {
                        var componentFile = ComponentProvider.GetPath(Project, deletedComponent);
                        File.Delete(componentFile);
                        var csFile = Path.Combine(Project.Location, $"{deletedComponent.FullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                        var csTypeDFile = Path.Combine(Project.ProjectComponentCodePath, $"{deletedComponent.FullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
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

                    File.Delete(ComponentProvider.GetPath(Project, oldFullName));
                    var csFile = Path.Combine(Project.Location, $"{oldFullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                    var csTypeDFile = Path.Combine(Project.ProjectComponentCodePath, $"{oldFullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
                    var csFileNew = Path.Combine(Project.Location, $"{renamedComponent.Item2.FullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                    var csTypeDFileNew = Path.Combine(Project.ProjectComponentCodePath, $"{renamedComponent.Item2.FullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
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

                if (!Project.IsClosing)
                    ProjectModel.BuildComponentTree(Project);
            });
        }

        private ComponentChildDTO CreateChildDTO(Component component)
        {
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
                    }).ToList(),
                Children = component.Children
                    .Where(child => !component.DefinitionChildren.Contains(child) || child.HasChanges)
                    .Select(CreateChildDTO)
                    .ToList()
            };
        }
    }
}
