using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TypeD.Components;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ComponentProvider : IComponentProvider
    {
        private static string ComponentFileEnding = "component";

        // Model
        IResourceModel ResourceModel { get; set; }
        IProjectModel ProjectModel { get; set; }
        ISaveModel SaveModel { get; set; }

        // Constructors
        public ComponentProvider() { }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            ProjectModel = ResourceModel.Get<IProjectModel>();
            SaveModel = ResourceModel.Get<ISaveModel>();
        }

        // Functions
        public T Create<T>(Project project, string className, string @namespace, Component parentComponent = null, List<string> interfaces = null) where T : ComponentTemplate
        {
            Type componentTemplateType = typeof(T);

            var component = TranslateComponentDTO(project, new ComponentDTO()
            {
                ClassName = className,
                Interfaces = interfaces ?? new List<string>(),
                Namespace = @namespace,
                ParentComponent = parentComponent?.FullName ?? "",
                TemplateClass = componentTemplateType.FullName
            });
            component.ParentComponent = parentComponent;

            //TODO: Look over this onces more
            var template = Activator.CreateInstance(componentTemplateType) as T;
            template.CreateCode(component);
            component.TypeOBaseType = template.Code.TypeOBaseType;
            ProjectModel.InitAndSaveCode(project, template.Code);
            template.Init();

            Save(project, component);

            ProjectModel.BuildComponentTree(project);
            return template;
        }

        public void Save(Project project, Component component)
        {
            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            componentSaveContext.Components.Add(component);
            SaveModel.AddSave<ComponentSaveContext>();
        }

        public Component Load(Project project, string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            var path = GetPath(project, fullName);

            return LoadFromPath(project, path);
        }

        private Component LoadFromPath(Project project, string path)
        {
            Component component = null;
            if (!File.Exists(path))
            {
                var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
                component = componentSaveContext.Components.Find(c => GetPath(project, c) == path);
                if (component == null) return null;
            }
            else
            {
                var dto = JSON.Deserialize<ComponentDTO>(path);
                component = TranslateComponentDTO(project, dto);
            }

            //TODO: Look over this onces more
            component.Template.CreateCode(component);
            ProjectModel.InitCode(project, component.Template.Code);
            component.Template.Init();

            return component;
        }

        private Component TranslateComponentDTO(Project project, ComponentDTO dto)
        {
            var component = new Component()
            {
                ClassName = dto.ClassName,
                Interfaces = dto.Interfaces.Select(
                                i => AppDomain.CurrentDomain.GetAssemblies()
                                    .SelectMany(a => a.GetTypes())
                                    .FirstOrDefault(t => t.FullName.Equals(i))).ToList(),
                Namespace = dto.Namespace,
                ParentComponent = Load(project, dto.ParentComponent),
                Template = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.FullName.Equals(dto.TemplateClass))) as ComponentTemplate,
                TypeOBaseType = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.FullName.Equals(dto.TypeOBaseType)),
                Children = dto.Children?.Select(c => Load(project, c)).ToList() ?? new List<Component>()
            };

            return component;
        }

        public void Delete(Project project, Component component)
        {
            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            componentSaveContext.Components.RemoveAll(c => c.FullName == component.FullName);
            componentSaveContext.DeletedComponents.Add(component);
            SaveModel.AddSave<ComponentSaveContext>();

            ProjectModel.BuildComponentTree(project);
        }

        public void Rename(Project project, Component component, string newClassName)
        {
            var oldClassname = component.ClassName;
            component.ClassName = newClassName;

            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            componentSaveContext.RenamedComponents.Add(new Tuple<string, Component>(oldClassname, component));
            SaveModel.AddSave<ComponentSaveContext>();

            Save(project, component);

            ProjectModel.BuildComponentTree(project);
        }

        public bool Exists(Project project, Component component)
        {
            return File.Exists(GetPath(project, component));
        }

        public bool Exists(Project project, Type type)
        {
            return File.Exists(GetPath(project, type.FullName));
        }

        public List<Component> ListAll(Project project)
        {
            var path = GetPath(project);
            if (!Directory.Exists(path)) return new List<Component>();

            var files = Directory.GetFiles(path, $"*.{ComponentFileEnding}", SearchOption.AllDirectories);
            var components = files.Select((f) => { return LoadFromPath(project, f); }).ToList();

            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            
            var retList = components.Union(componentSaveContext.Components)
                                    .GroupBy(t => t.FullName)
                                    .Select(t => t.First())
                                    .ToList();
            retList.RemoveAll(c => componentSaveContext.DeletedComponents.Exists(d => c.FullName == d.FullName));
            retList.RemoveAll(c => componentSaveContext.RenamedComponents.Exists(r => c.FullName == $"{r.Item2.Namespace}.{r.Item1}"));
            return retList;
        }

        public string GetPath(Project project, Component component)
        {
            return GetPath(project, component.FullName);
        }
        public string GetPath(Project project, string fullName)
        {
            return Path.Combine(GetPath(project), $"{fullName.Replace('.', Path.DirectorySeparatorChar)}.{ComponentFileEnding}");
        }

        // Internal
        private string GetPath(Project project)
        {
            return Path.Combine(project.ProjectTypeOPath, "components");
        }
    }
}
