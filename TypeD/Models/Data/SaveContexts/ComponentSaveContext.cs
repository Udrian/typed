using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TypeD.Helpers;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Data.SaveContexts
{
    public class ComponentSaveContext : SaveContext
    {
        // Models
        IProjectModel ProjectModel { get; set; }

        // Providers
        IComponentProvider ComponentProvider { get; set; }

        // Properties
        public Project Project { get; set; }
        public List<Component> Components { get; set; }
        public List<Component> DeletedComponents { get; set; }
        public List<Tuple<string, Component>> RenamedComponents { get; set; }

        // Constructors
        public override void Init(IResourceModel resourceModel, object param = null)
        {
            Components = new List<Component>();
            DeletedComponents = new List<Component>();
            RenamedComponents = new List<Tuple<string, Component>>();
            if (param is Project)
                Project = param as Project;

            ProjectModel = resourceModel.Get<IProjectModel>();
            ComponentProvider = resourceModel.Get<IComponentProvider>();
        }

        // Functions
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
                        ParentComponent = saveComponent.ParentComponent?.FullName ?? "",
                        TemplateClass = saveComponent.Template.GetType().FullName,
                        TypeOBaseType = saveComponent.TypeOBaseType.FullName,
                        Children = saveComponent.Children.Select(c => c.FullName).ToList()
                    }, ComponentProvider.GetPath(Project, saveComponent));
                }

                foreach (var deletedComponent in DeletedComponents)
                {
                    if (ComponentProvider.Exists(Project, deletedComponent))
                    {
                        File.Delete(ComponentProvider.GetPath(Project, deletedComponent));
                        var csFile = Path.Combine(Project.Location, $"{deletedComponent.FullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                        var csTypeDFile = Path.Combine(Project.Location, $"{deletedComponent.FullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
                        if (File.Exists(csFile))
                            File.Delete(csFile);
                        if (File.Exists(csTypeDFile))
                            File.Delete(csTypeDFile);
                    }
                }

                foreach (var renamedComponent in RenamedComponents)
                {
                    var oldFullName = $"{renamedComponent.Item2.Namespace}.{renamedComponent.Item1}";

                    File.Delete(ComponentProvider.GetPath(Project, oldFullName));
                    var csFile = Path.Combine(Project.Location, $"{oldFullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                    var csTypeDFile = Path.Combine(Project.Location, $"{oldFullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
                    var csFileNew = Path.Combine(Project.Location, $"{renamedComponent.Item2.FullName.Replace('.', Path.DirectorySeparatorChar)}.cs");
                    var csTypeDFileNew = Path.Combine(Project.Location, $"{renamedComponent.Item2.FullName.Replace('.', Path.DirectorySeparatorChar)}.typed.cs");
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
    }
}
