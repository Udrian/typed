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
        IComponentModel ComponentModel { get; set; }

        // Data
        List<Component> BaseTypeComponents { get; set; }

        // Constructors
        public ComponentProvider()
        {
            BaseTypeComponents = new List<Component>();
        }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            ProjectModel = ResourceModel.Get<IProjectModel>();
            SaveModel = ResourceModel.Get<ISaveModel>();
            ComponentModel = ResourceModel.Get<IComponentModel>();
        }

        // Functions
        public ComponentTemplate Create(Project project, string className, string @namespace, Component parentComponent, List<string> interfaces = null)
        {
            var component = TranslateComponentDTO(project, new ComponentDTO()
            {
                ClassName = className,
                Interfaces = interfaces ?? new List<string>(),
                Namespace = @namespace,
                ParentComponent = parentComponent.FullName,
                TemplateClass = parentComponent.Template.GetType().FullName
            });

            component.Template.Init();
            //TODO: Remove TypeOBaseType
            component.TypeOBaseType = ComponentModel.GetBaseType(component);
            ProjectModel.InitAndSaveCode(project, component.Template.Code);

            Save(project, component);

            ProjectModel.BuildComponentTree(project);
            return component.Template;
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

            var component = LoadFromPath(project, path);
            if (component == null)
            {
                component = BaseTypeComponents.Find(c => c.FullName == fullName);
            }
            return component;
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
            component.Template.Init();
            ProjectModel.InitCode(project, component.Template.Code);

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
                Children = dto.Children?.Select(c => Load(project, c)).ToList() ?? new List<Component>()
            };
            component.Template.Component = component;

            //TODO: Remove TypeOBaseType
            component.TypeOBaseType = ComponentModel.GetBaseType(component);
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
            var components = ListAll(project);
            return components.Exists(c => c.FullName == type.FullName);
        }

        public List<Component> ListAll(Project project)
        {
            var path = GetPath(project);
            List<Component> components;
            if (!Directory.Exists(path))
            {
                components = new List<Component>();
            }
            else
            {
                var files = Directory.GetFiles(path, $"*.{ComponentFileEnding}", SearchOption.AllDirectories);
                components = files.Select((f) => { return LoadFromPath(project, f); }).ToList();
            }

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

        public void AddBaseTypeComponent(Component component)
        {
            BaseTypeComponents.Add(component);
        }
        public void RemoveBaseTypeComponent(Component component)
        {
            BaseTypeComponents.RemoveAll(c => c.FullName == component.FullName);
        }

        public List<Component> GetBaseTypeComponents()
        {
            return new List<Component>(BaseTypeComponents);
        }

        // Internal
        private string GetPath(Project project)
        {
            return Path.Combine(project.ProjectTypeOPath, "components");
        }
    }
}
