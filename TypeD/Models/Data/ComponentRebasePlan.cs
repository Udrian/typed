using System.Text.Json;
using TypeD.Components;
using TypeD.Helpers;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data
{
    /// <summary>Prepares a reversible base-type change for component definitions and instances.</summary>
    public sealed class ComponentRebasePlan
    {
        private sealed record PropertyState(Property Item, string Name, Type Type, string Description, string Group,
            string DeclaringType, Component FromComponent, Type TemplateType, bool ReadOnly, bool GenerateCode,
            bool Hidden, Func<Component, IEnumerable<string>> DynamicOptions, string Json);

        private sealed class State
        {
            public Component Component;
            public Component Base;
            public ComponentTemplate Template;
            public List<Type> Interfaces;
            public Type[] InterfaceValues;
            public List<Property> Properties;
            public List<Property> Baselines;
            public List<PropertyDTO> Unresolved;
            public string UnresolvedJson;
            public List<PreservedPropertyDTO> Preserved;
            public string PreservedJson;
            public HashSet<string> Persisted;
            public string[] PersistedNames;
            public List<PropertyState> PropertyStates;
        }

        private sealed record Change(State Before, Component NewBase, List<Type> Interfaces,
            List<Property> Properties, List<Property> Baselines, List<PropertyDTO> Unresolved,
            List<PreservedPropertyDTO> Preserved, HashSet<string> Persisted);

        private readonly IProjectModel ProjectModel;
        private readonly IPropertyModel PropertyModel;
        private readonly Project Project;
        private readonly Type TemplateType;
        private readonly List<Change> Changes;
        private readonly List<State> BaseStates;
        private List<State> Applied;

        /// <summary>Gets the roots that contain a changed definition or instance.</summary>
        public IReadOnlyList<Component> AffectedRoots { get; }
        /// <summary>Gets the changed definitions and instances, including derived types.</summary>
        public IReadOnlyList<Component> AffectedComponents { get; }
        /// <summary>Gets the property overrides retained because the new base has no matching property.</summary>
        public IReadOnlyList<string> PreservedOverrides { get; }

        /// <summary>Prepares the migration without changing any component or file.</summary>
        public ComponentRebasePlan(Project project, IProjectModel projectModel, IPropertyModel propertyModel,
            IEnumerable<Component> roots, string componentName, Component newBase, IEnumerable<Type> interfaces)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(projectModel);
            ArgumentNullException.ThrowIfNull(propertyModel);
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentException.ThrowIfNullOrEmpty(componentName);
            ArgumentNullException.ThrowIfNull(newBase);
            ArgumentNullException.ThrowIfNull(interfaces);
            Project = project;
            ProjectModel = projectModel;
            PropertyModel = propertyModel;
            TemplateType = newBase.Template?.GetType()
                ?? throw new InvalidOperationException("The selected base type has no component template.");
            var interfaceList = interfaces.ToList();
            if (interfaceList.Any(type => type == null))
                throw new ArgumentException("Component interfaces cannot contain null.", nameof(interfaces));
            if (Inheritance(newBase).Any(component => component.FullName == componentName))
                throw new InvalidOperationException("A component cannot inherit from itself or its descendants.");
            BaseStates = Inheritance(newBase).Select(Capture).ToList();

            var rootList = roots.Distinct().ToList();
            var nodes = rootList.SelectMany(Traverse).Distinct().ToList();
            var affected = nodes.Where(node => Inheritance(node).Any(parent => parent.FullName == componentName)).ToList();
            if (!affected.Any(node => node.FullName == componentName))
                throw new InvalidOperationException($"Component '{componentName}' was not found.");
            if (affected.Where(node => node.FullName == componentName)
                .Select(node => node.BaseInheritedComponent?.FullName).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new InvalidOperationException($"Copies of '{componentName}' have inconsistent base types.");
            if (newBase.Properties.GroupBy(property => property.Name, StringComparer.Ordinal).Any(group => group.Count() > 1))
                throw new InvalidOperationException("The selected base type has duplicate property names.");

            var preserved = new List<string>();
            Changes = new List<Change>();
            foreach (var node in affected)
            {
                var target = Inheritance(node).First(parent => parent.FullName == componentName);
                var oldBase = target.BaseInheritedComponent
                    ?? throw new InvalidOperationException($"Component '{componentName}' has no base type.");
                var oldNames = Inheritance(oldBase).Select(parent => parent.FullName).ToHashSet(StringComparer.Ordinal);
                var oldPropertyNames = oldBase.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
                bool Inherited(Property property)
                {
                    var declaring = property.DeclaringTypeName ?? property.FromComponent?.FullName;
                    if (declaring != null)
                        return oldNames.Contains(declaring) ||
                            (!node.CustomProperties.Any(custom => custom.Name == property.Name) &&
                             oldBase.Properties.Any(inherited => inherited.Name == property.Name &&
                                 (inherited.DeclaringTypeName ?? inherited.FromComponent?.FullName) == declaring));
                    return oldPropertyNames.Contains(property.Name) &&
                        !node.CustomProperties.Any(custom => custom.Name == property.Name);
                }

                if (node.Properties.GroupBy(property => property.Name, StringComparer.Ordinal).Any(group => group.Count() > 1))
                    throw new InvalidOperationException($"Component '{node.FullName}' has duplicate property names.");
                var oldInherited = node.Properties.Where(Inherited).ToDictionary(property => property.Name, StringComparer.Ordinal);
                var properties = node.Properties.Where(property => !Inherited(property)).Select(property => new Property(property)).ToList();
                var baselines = node.OveriddenProperties.Where(property => !Inherited(property)).Select(property => new Property(property)).ToList();
                var unresolved = node.UnresolvedPropertyOverrides
                    .Select(property => new PropertyDTO { Name = property.Name, Value = property.Value }).ToList();
                var archived = node.PreservedPropertyOverrides.ToList();
                var persisted = new HashSet<string>(node.PersistedPropertyOverrides, StringComparer.Ordinal);
                var incomingNames = newBase.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
                foreach (var old in oldInherited.Values.Where(property => !incomingNames.Contains(property.Name)))
                {
                    if (old.Name == "ID")
                    {
                        properties.Add(new Property(old) { GenerateCode = false });
                        var baseline = node.OveriddenProperties.FirstOrDefault(property => property.Name == old.Name);
                        if (baseline != null)
                            baselines.Add(new Property(baseline));
                        continue;
                    }
                    if (unresolved.Any(item => item.Name == old.Name))
                        archived.Add(Archive(old));
                    else
                        unresolved.Add(new PropertyDTO { Name = old.Name, Value = SerializeValue(old) });
                    preserved.Add($"{node.FullName}: {old.Name}");
                }

                foreach (var incoming in newBase.Properties)
                {
                    var retained = properties.FirstOrDefault(property => property.Name == incoming.Name);
                    if (retained != null)
                    {
                        if (incoming.Name == "ID" && retained.Type == incoming.Type)
                            continue;
                        throw new InvalidOperationException(
                            $"The property '{incoming.Name}' on '{node.FullName}' conflicts with the new base type.");
                    }
                    var missing = unresolved.FirstOrDefault(property => property.Name == incoming.Name);
                    if (missing != null)
                    {
                        archived.Add(new PreservedPropertyDTO { Name = missing.Name, Value = missing.Value });
                        unresolved.Remove(missing);
                        persisted.Remove(missing.Name);
                        preserved.Add($"{node.FullName}: {missing.Name}");
                    }
                    var property = new Property(incoming) { Value = CopyValue(incoming) };
                    var baseline = new Property(incoming) { Value = CopyValue(incoming) };
                    if (oldInherited.TryGetValue(incoming.Name, out var old))
                    {
                        var keepValue = old.Name == "ID" || node.HasPropertyOverridesFor(old.Name)
                            || node.HasPersistedPropertyOverride(old.Name);
                        if (keepValue)
                        {
                            if (old.Type != incoming.Type)
                            {
                                if (old.Name == "ID")
                                    throw new InvalidOperationException($"The ID property on '{node.FullName}' has an incompatible type.");
                                archived.Add(Archive(old));
                                persisted.Remove(old.Name);
                                preserved.Add($"{node.FullName}: {old.Name}");
                            }
                            else
                                property.Value = CopyValue(old);
                        }
                    }
                    properties.Add(property);
                    baselines.Add(baseline);
                }
                Changes.Add(new Change(Capture(node), node.FullName == componentName ? newBase : node.BaseInheritedComponent,
                    node.FullName == componentName ? new List<Type>(interfaceList) : new List<Type>(node.Interfaces),
                    properties, baselines, unresolved, archived, persisted));
            }
            AffectedComponents = affected;
            var affectedSet = affected.ToHashSet();
            AffectedRoots = rootList.Where(root => Traverse(root).Any(affectedSet.Contains)).ToList();
            PreservedOverrides = preserved;
        }

        /// <summary>Applies the migration, rejecting stale components and restoring state if initialization fails.</summary>
        public void Apply()
        {
            if (Applied != null)
                throw new InvalidOperationException("The component rebase is already applied.");
            Validate(BaseStates);
            Validate(Changes.Select(change => change.Before));
            try
            {
                foreach (var change in Changes)
                {
                    var node = change.Before.Component;
                    node.BaseInheritedComponent = change.NewBase;
                    node.Interfaces = change.Interfaces;
                    node.Properties = change.Properties;
                    node.OveriddenProperties = change.Baselines;
                    node.UnresolvedPropertyOverrides = change.Unresolved;
                    node.PreservedPropertyOverrides = change.Preserved;
                    node.PersistedPropertyOverrides = change.Persisted;
                }
                foreach (var change in Changes)
                {
                    var node = change.Before.Component;
                    var template = Activator.CreateInstance(TemplateType) as ComponentTemplate
                        ?? throw new InvalidOperationException($"Could not create component template '{TemplateType.FullName}'.");
                    template.Component = node;
                    template.Init();
                    ProjectModel.InitCode(Project, template.Code);
                    node.Template = template;
                }
                Applied = Changes.Select(change => Capture(change.Before.Component)).ToList();
            }
            catch
            {
                foreach (var change in Changes)
                    Restore(change.Before);
                throw;
            }
        }

        /// <summary>Restores the original base, values, interfaces, and templates.</summary>
        public void Undo()
        {
            if (Applied == null)
                throw new InvalidOperationException("The component rebase has not been applied.");
            Validate(Applied);
            foreach (var state in Changes.Select(change => change.Before))
                if (!StoredStateMatches(state))
                    throw new InvalidOperationException($"The original state of '{state.Component.FullName}' changed since the rebase.");
            foreach (var change in Changes)
                Restore(change.Before);
            Applied = null;
        }

        private State Capture(Component component) => new State
        {
            Component = component,
            Base = component.BaseInheritedComponent,
            Template = component.Template,
            Interfaces = component.Interfaces,
            InterfaceValues = component.Interfaces.ToArray(),
            Properties = component.Properties,
            Baselines = component.OveriddenProperties,
            Unresolved = component.UnresolvedPropertyOverrides,
            UnresolvedJson = JsonSerializer.Serialize(component.UnresolvedPropertyOverrides, JSON.CreateOptions()),
            Preserved = component.PreservedPropertyOverrides,
            PreservedJson = JsonSerializer.Serialize(component.PreservedPropertyOverrides, JSON.CreateOptions()),
            Persisted = component.PersistedPropertyOverrides,
            PersistedNames = component.PersistedPropertyOverrides.ToArray(),
            PropertyStates = component.Properties.Concat(component.OveriddenProperties).Select(property =>
                new PropertyState(property, property.Name, property.Type, property.Description, property.Group,
                    property.DeclaringTypeName, property.FromComponent, property.PropertyTemplateType, property.ReadOnly,
                    property.GenerateCode, property.Hidden, property.DynamicOptions, SerializeValue(property).GetRawText())).ToList()
        };

        private void Validate(IEnumerable<State> states)
        {
            foreach (var state in states)
            {
                var node = state.Component;
                if (!ReferenceEquals(node.BaseInheritedComponent, state.Base) || !ReferenceEquals(node.Template, state.Template)
                    || !ReferenceEquals(node.Interfaces, state.Interfaces) || !ReferenceEquals(node.Properties, state.Properties)
                    || !ReferenceEquals(node.OveriddenProperties, state.Baselines)
                    || !ReferenceEquals(node.UnresolvedPropertyOverrides, state.Unresolved)
                    || !ReferenceEquals(node.PreservedPropertyOverrides, state.Preserved)
                    || !ReferenceEquals(node.PersistedPropertyOverrides, state.Persisted)
                    || !StoredStateMatches(state))
                    throw new InvalidOperationException($"Component '{node.FullName}' changed since the rebase was prepared.");
            }
        }

        private bool StoredStateMatches(State state) =>
            state.Interfaces.SequenceEqual(state.InterfaceValues)
            && JsonSerializer.Serialize(state.Unresolved, JSON.CreateOptions()) == state.UnresolvedJson
            && JsonSerializer.Serialize(state.Preserved, JSON.CreateOptions()) == state.PreservedJson
            && state.Persisted.SetEquals(state.PersistedNames)
            && state.Properties.Concat(state.Baselines).SequenceEqual(state.PropertyStates.Select(item => item.Item))
            && state.PropertyStates.All(item => item.Item.Name == item.Name && item.Item.Type == item.Type
                && item.Item.Description == item.Description && item.Item.Group == item.Group
                && item.Item.DeclaringTypeName == item.DeclaringType
                && ReferenceEquals(item.Item.FromComponent, item.FromComponent)
                && item.Item.PropertyTemplateType == item.TemplateType && item.Item.ReadOnly == item.ReadOnly
                && item.Item.GenerateCode == item.GenerateCode && item.Item.Hidden == item.Hidden
                && item.Item.DynamicOptions == item.DynamicOptions && SerializeValue(item.Item).GetRawText() == item.Json);

        private static void Restore(State state)
        {
            state.Component.BaseInheritedComponent = state.Base;
            state.Component.Template = state.Template;
            state.Component.Interfaces = state.Interfaces;
            state.Component.Properties = state.Properties;
            state.Component.OveriddenProperties = state.Baselines;
            state.Component.UnresolvedPropertyOverrides = state.Unresolved;
            state.Component.PreservedPropertyOverrides = state.Preserved;
            state.Component.PersistedPropertyOverrides = state.Persisted;
        }

        private Type StoredType(Property property)
        {
            if (property.Type == null)
                return property.Value?.GetType() ?? typeof(object);
            var definition = PropertyModel.Get(property.Type);
            return definition != null && definition.ValueType != definition.Type ? definition.ValueType : property.Type;
        }

        private JsonElement SerializeValue(Property property) =>
            JsonSerializer.SerializeToElement(property.Value, StoredType(property), JSON.CreateOptions());

        private PreservedPropertyDTO Archive(Property property) => new()
        {
            Name = property.Name,
            TypeName = StoredType(property).FullName,
            DeclaringTypeName = property.DeclaringTypeName ?? property.FromComponent?.FullName,
            Value = SerializeValue(property)
        };

        private object CopyValue(Property property) =>
            SerializeValue(property).Deserialize(StoredType(property), JSON.CreateOptions());

        private static IEnumerable<Component> Inheritance(Component component)
        {
            var visited = new HashSet<Component>();
            for (var current = component; current != null; current = current.BaseInheritedComponent)
            {
                if (!visited.Add(current))
                    throw new InvalidOperationException("The component inheritance chain contains a cycle.");
                yield return current;
            }
        }

        private static IEnumerable<Component> Traverse(Component root)
        {
            var visited = new HashSet<Component>();
            var pending = new Stack<Component>();
            pending.Push(root);
            while (pending.TryPop(out var current))
            {
                if (!visited.Add(current))
                    continue;
                yield return current;
                if (current.BaseInheritedComponent != null)
                    pending.Push(current.BaseInheritedComponent);
                foreach (var child in current.Children)
                    pending.Push(child);
            }
        }
    }
}
