using System.Text.Json;
using TypeD.Components;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
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
        IHookModel HookModel { get; set; }
        IPropertyModel PropertyModel { get; set; }
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
            HookModel = ResourceModel.Get<IHookModel>();
            PropertyModel = ResourceModel.Get<IPropertyModel>();
        }

        private Type GetStoredValueType(Type propertyType)
        {
            var definition = PropertyModel?.Get(propertyType);
            return definition != null && definition.ValueType != definition.Type ? definition.ValueType : propertyType;
        }

        // Functions
        public ComponentTemplate Create(Project project, string className, string @namespace, Component parentComponent, List<string> interfaces = null)
        {
            var component = TranslateComponentDTO(project, new ComponentDTO()
            {
                ClassName = className,
                Interfaces = interfaces ?? new List<string>(),
                Namespace = @namespace,
                BaseInheritedComponent = parentComponent.FullName,
                TemplateClass = parentComponent.Template.GetType().FullName
            });

            var extractPropertiesHook = HookModel.Shoot(new ExtractPropertiesHook(parentComponent));
            component.Properties = extractPropertiesHook.Properties;

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
            componentSaveContext.DeletedComponents.RemoveAll(c => c.FullName == component.FullName);
            componentSaveContext.Components.RemoveAll(c => c.FullName == component.FullName);
            componentSaveContext.Components.Add(component);
            SaveModel.AddSave<ComponentSaveContext>();
        }

        public Component CreateInstance(Project project, Component component)
        {
            return CloneComponent(project, component, new Dictionary<Component, Component>());
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
                BaseInheritedComponent = Load(project, dto.BaseInheritedComponent),
                Template = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.FullName.Equals(dto.TemplateClass))) as ComponentTemplate,
                Children = dto.Children?.Select(c => TranslateChildDTO(project, c)).ToList() ?? new List<Component>(),
                Properties = dto.Properties?.Select(p =>
                {
                    return new Property()
                    {
                        Name = p.Name,
                        Value = p.Value
                    };
                }).ToList() ?? new List<Property>()
            };

            component.DefinitionChildren = new List<Component>(component.Children);
            foreach (var child in component.Children)
            {
                child.ParentComponent = component;
            }

            var extractPropertiesHook = HookModel.Shoot(new ExtractPropertiesHook(component));
            var defaultProperties = extractPropertiesHook.Properties;
            component.Properties = defaultProperties.Select((d) =>
            {
                var newProp = component.Properties.FirstOrDefault(p => p.Name == d.Name);
                if (newProp != null && newProp.Value is JsonElement)
                {
                    var val = ((JsonElement)newProp.Value).Deserialize(GetStoredValueType(d.Type), JSON.CreateOptions(web: true));
                    if(!Equals(d.Value, newProp.Value))
                    {
                        d.Value = val;
                    }
                }
                return d;
            }).ToList();
            component.OveriddenProperties = component.Properties.Select(property => new Property(property)).ToList();

            component.Template.Component = component;

            //TODO: Remove TypeOBaseType
            component.TypeOBaseType = ComponentModel.GetBaseType(component);
            return component;
        }

        private Component TranslateChildDTO(Project project, ComponentChildDTO dto)
        {
            var definition = Load(project, dto.FullName);
            if (definition == null)
            {
                throw new InvalidDataException($"Child component '{dto.FullName}' could not be loaded.");
            }

            var child = CreateInstance(project, definition);
            ApplyChildDTO(project, child, dto);
            return child;
        }

        private void ApplyChildDTO(Project project, Component child, ComponentChildDTO dto)
        {
            if (!string.IsNullOrEmpty(dto.ID))
            {
                child.ID = dto.ID;
            }
            child.Removed = dto.Removed;

            var propertyOverrides = dto.Properties ?? new List<PropertyDTO>();
            child.Properties = child.Properties.Select(property =>
            {
                if (property.Name == "ID" && !string.IsNullOrEmpty(dto.ID))
                {
                    property.Value = dto.ID;
                    return property;
                }

                var propertyOverride = propertyOverrides.FirstOrDefault(p => p.Name == property.Name);
                if (propertyOverride?.Value is JsonElement jsonValue)
                {
                    property.Value = jsonValue.Deserialize(GetStoredValueType(property.Type), JSON.CreateOptions(web: true));
                }
                else if (propertyOverride != null)
                {
                    property.Value = propertyOverride.Value;
                }
                return property;
            }).ToList();

            if (dto.Children != null)
            {
                child.Children.RemoveAll(nested => !child.DefinitionChildren.Contains(nested));
                var matchedDefinitionChildren = new HashSet<Component>();
                foreach (var nestedDTO in dto.Children)
                {
                    var nestedChild = child.Children.FirstOrDefault(nested =>
                        !matchedDefinitionChildren.Contains(nested) &&
                        nested.FullName == nestedDTO.FullName &&
                        (string.IsNullOrEmpty(nestedDTO.ID) || nested.ID == nestedDTO.ID));

                    if (nestedChild != null)
                    {
                        matchedDefinitionChildren.Add(nestedChild);
                        ApplyChildDTO(project, nestedChild, nestedDTO);
                    }
                    else
                    {
                        nestedChild = TranslateChildDTO(project, nestedDTO);
                        nestedChild.ParentComponent = child;
                        child.Children.Add(nestedChild);
                    }
                }
            }

            foreach (var nestedChild in child.Children)
            {
                nestedChild.ParentComponent = child;
            }
        }

        private Component CloneComponent(Project project, Component source, Dictionary<Component, Component> clones, bool useCurrentValuesAsOverrideBaseline = false)
        {
            if (clones.TryGetValue(source, out var existingClone))
            {
                return existingClone;
            }

            var clone = new Component
            {
                ClassName = source.ClassName,
                Namespace = source.Namespace,
                BaseInheritedComponent = source.BaseInheritedComponent,
                Interfaces = new List<Type>(source.Interfaces),
                TypeOBaseType = source.TypeOBaseType,
                Properties = source.Properties.Select(property => new Property(property)).ToList(),
                OveriddenProperties = (useCurrentValuesAsOverrideBaseline ? source.Properties : source.OveriddenProperties)
                    .Select(property => new Property(property)).ToList(),
                Template = Activator.CreateInstance(source.Template.GetType()) as ComponentTemplate,
                Removed = source.Removed
            };
            clones.Add(source, clone);

            clone.Children = source.Children
                .Select(child => CloneComponent(project, child, clones, true))
                .ToList();
            clone.DefinitionChildren = source.DefinitionChildren
                .Select(child => CloneComponent(project, child, clones, true))
                .ToList();

            foreach (var child in clone.Children)
            {
                child.ParentComponent = clone;
            }

            if (clone.Template == null)
            {
                throw new InvalidOperationException($"Component template '{source.Template.GetType().FullName}' could not be cloned.");
            }

            clone.Template.Component = clone;
            clone.Template.Init();
            ProjectModel.InitCode(project, clone.Template.Code);
            return clone;
        }

        public void Delete(Project project, Component component)
        {
            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            var wasPendingCreate = componentSaveContext.Components.RemoveAll(c => c.FullName == component.FullName) > 0;
            componentSaveContext.RenamedComponents.RemoveAll(c => ReferenceEquals(c.Item2, component));
            componentSaveContext.DeletedComponents.RemoveAll(c => c.FullName == component.FullName);
            if (wasPendingCreate && !Exists(project, component))
            {
                if (SaveModel.SaveContextExists<CodeSaveContext>())
                {
                    var codeSaveContext = SaveModel.GetSaveContext<CodeSaveContext>();
                    codeSaveContext.Codes.RemoveAll(code =>
                        code.FilePath() == component.Template.Code.FilePath());
                }
            }
            else
            {
                componentSaveContext.DeletedComponents.Add(component);
            }
            SaveModel.AddSave<ComponentSaveContext>();

            ProjectModel.BuildComponentTree(project);
        }

        public void Restore(Project project, Component component)
        {
            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            componentSaveContext.DeletedComponents.RemoveAll(c => c.FullName == component.FullName);
            if (!componentSaveContext.Components.Any(c => c.FullName == component.FullName))
                componentSaveContext.Components.Add(component);

            ProjectModel.InitAndSaveCode(project, component.Template.Code);
            SaveModel.AddSave<ComponentSaveContext>();
            ProjectModel.BuildComponentTree(project);
        }

        public void Rename(Project project, Component component, string newClassName)
        {
            var oldClassname = component.ClassName;
            component.ClassName = newClassName;

            var componentSaveContext = SaveModel.GetSaveContext<ComponentSaveContext>(project);
            var renameIndex = componentSaveContext.RenamedComponents.FindIndex(rename =>
                ReferenceEquals(rename.Item2, component));
            if (renameIndex >= 0)
            {
                var originalClassName = componentSaveContext.RenamedComponents[renameIndex].Item1;
                if (originalClassName == newClassName)
                    componentSaveContext.RenamedComponents.RemoveAt(renameIndex);
                else
                    componentSaveContext.RenamedComponents[renameIndex] =
                        new Tuple<string, Component>(originalClassName, component);
            }
            else if (oldClassname != newClassName)
            {
                componentSaveContext.RenamedComponents.Add(new Tuple<string, Component>(oldClassname, component));
            }
            if (oldClassname != newClassName)
                SaveModel.AddSave<ComponentSaveContext>();

            if (oldClassname != newClassName)
                Save(project, component);

            if (oldClassname != newClassName)
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
            var extractPropertiesHook = HookModel.Shoot(new ExtractPropertiesHook(component));
            var defaultProperties = extractPropertiesHook.Properties;
            component.Properties = defaultProperties.Select((d) =>
            {
                var newProp = component.Properties.FirstOrDefault(p => p.Name == d.Name);
                if (newProp != null && newProp.Value is JsonElement)
                {
                    var val = ((JsonElement)newProp.Value).Deserialize(GetStoredValueType(d.Type), JSON.CreateOptions(web: true));
                    if (!Equals(d.Value, newProp.Value))
                    {
                        d.Value = val;
                    }
                }
                return d;
            }).ToList();
            component.OveriddenProperties = component.Properties.Select(property => new Property(property)).ToList();

            component.ReadOnly = true;
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
