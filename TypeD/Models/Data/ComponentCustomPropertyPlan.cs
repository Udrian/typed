using System.Text.Json;
using TypeD.Helpers;
using TypeD.Models.DTO;
using TypeD.Models.Interfaces;

namespace TypeD.Models.Data
{
    /// <summary>Prepares a reversible batch of custom-property edits on a component and its copies.</summary>
    public sealed class ComponentCustomPropertyPlan
    {
        private sealed record PropertyState(Property Item, Property Copy, string Value);
        private sealed class State
        {
            public Component Component;
            public Component Base;
            public string FullName;
            public List<Property> Properties;
            public List<Property> Baselines;
            public List<CustomProperty> Customs;
            public List<string> Removed;
            public List<PropertyDTO> Unresolved;
            public List<PreservedPropertyDTO> Preserved;
            public HashSet<string> Persisted;
            public string CustomJson;
            public string RemovedJson;
            public string UnresolvedJson;
            public string PreservedJson;
            public string[] PersistedNames;
            public List<PropertyState> PropertyStates;
        }

        private readonly IPropertyModel PropertyModel;
        private readonly List<State> Before;
        private readonly List<State> After;
        private bool Applied;

        /// <summary>Gets components affected by the edits.</summary>
        public IReadOnlyList<Component> AffectedComponents { get; }
        /// <summary>Gets containing component roots that require saving.</summary>
        public IReadOnlyList<Component> AffectedRoots { get; }
        /// <summary>Gets descriptions of values retained for manual repair.</summary>
        public IReadOnlyList<string> PreservedValues { get; }

        /// <summary>Prepares all property changes without mutating component data.</summary>
        /// <param name="roots">Project component roots.</param>
        /// <param name="declaringType">The edited component's full name.</param>
        /// <param name="properties">Desired properties, with the original name or null for a new property.</param>
        /// <param name="propertyModel">Property type definitions and serializers.</param>
        public ComponentCustomPropertyPlan(IEnumerable<Component> roots, string declaringType,
            IEnumerable<(string OriginalName, CustomProperty Property, Type Type)> properties, IPropertyModel propertyModel)
        {
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentException.ThrowIfNullOrEmpty(declaringType);
            ArgumentNullException.ThrowIfNull(properties);
            PropertyModel = propertyModel ?? throw new ArgumentNullException(nameof(propertyModel));
            var desired = properties.ToList();
            if (desired.Any(item => item.Property == null || item.Type == null
                || item.Property.TypeName != item.Type.FullName || string.IsNullOrEmpty(item.Property.Name))
                || desired.Select(item => item.Property.Name).Distinct(StringComparer.Ordinal).Count() != desired.Count
                || desired.Where(item => item.OriginalName != null).Select(item => item.OriginalName)
                    .Distinct(StringComparer.Ordinal).Count() != desired.Count(item => item.OriginalName != null))
                throw new ArgumentException("Custom property names and types must be valid and unique.", nameof(properties));

            var rootList = roots.Distinct().ToList();
            var nodes = rootList.SelectMany(Traverse).Distinct().ToList();
            var definition = nodes.FirstOrDefault(node => node.FullName == declaringType)
                ?? throw new InvalidOperationException($"Component '{declaringType}' was not found.");
            var originalNames = definition.CustomProperties.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
            if (originalNames.Count != definition.CustomProperties.Count
                || desired.Any(item => item.OriginalName != null && !originalNames.Contains(item.OriginalName)))
                throw new InvalidOperationException("The original custom properties are inconsistent.");
            var mapping = desired.Where(item => item.OriginalName != null)
                .ToDictionary(item => item.OriginalName, StringComparer.Ordinal);
            var newNames = desired.Select(item => item.Property.Name).ToHashSet(StringComparer.Ordinal);
            var preserved = new List<string>();
            Before = new List<State>();
            After = new List<State>();

            foreach (var node in nodes)
            {
                var declaring = FindDeclaring(node, declaringType);
                if (declaring == null)
                    continue;
                var source = declaring.CustomProperties;
                if (!source.Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(originalNames))
                    throw new InvalidOperationException($"Copies of '{declaringType}' have inconsistent custom properties.");
                var old = source.ToDictionary(item => item.Name, StringComparer.Ordinal);
                bool IsEdited(Property item) => originalNames.Contains(item.Name)
                    && (item.DeclaringTypeName ?? item.FromComponent?.FullName) == declaringType;
                var retained = node.Properties.Where(item => !IsEdited(item)).ToList();
                if (retained.Any(item => newNames.Contains(item.Name))
                    || (node.FullName != declaringType && node.CustomProperties.Any(item => newNames.Contains(item.Name))))
                    throw new InvalidOperationException($"A property on '{node.FullName}' conflicts with the edited custom properties.");
                var resultProperties = new List<Property>(retained);
                var resultBaselines = node.OveriddenProperties.Where(item => !IsEdited(item)).ToList();
                var unresolved = node.UnresolvedPropertyOverrides.ToList();
                var archive = node.PreservedPropertyOverrides.ToList();
                var persisted = new HashSet<string>(node.PersistedPropertyOverrides, StringComparer.Ordinal);

                foreach (var existing in node.Properties.Where(IsEdited))
                {
                    if (!mapping.TryGetValue(existing.Name, out var item)
                        || item.Property.Access != CustomPropertyAccess.Public || existing.Type != item.Type)
                    {
                        archive.Add(Archive(existing, declaringType));
                        preserved.Add($"{node.FullName}: {existing.Name}");
                    }
                }
                foreach (var missing in unresolved.Where(item => newNames.Contains(item.Name)).ToList())
                {
                    archive.Add(new PreservedPropertyDTO { Name = missing.Name, Value = missing.Value });
                    unresolved.Remove(missing);
                    persisted.Remove(missing.Name);
                    preserved.Add($"{node.FullName}: {missing.Name}");
                }

                var persistedBefore = persisted.ToArray();
                foreach (var name in originalNames)
                    persisted.Remove(name);
                foreach (var name in persistedBefore.Where(originalNames.Contains))
                    if (mapping.TryGetValue(name, out var item) && item.Property.Access == CustomPropertyAccess.Public
                        && old[name].TypeName == item.Property.TypeName)
                        persisted.Add(item.Property.Name);

                foreach (var item in desired.Where(item => item.Property.Access == CustomPropertyAccess.Public))
                {
                    var existing = item.OriginalName == null ? null : node.Properties.FirstOrDefault(property =>
                        property.Name == item.OriginalName && IsEdited(property));
                    var baseline = item.OriginalName == null ? null : node.OveriddenProperties.FirstOrDefault(property =>
                        property.Name == item.OriginalName && IsEdited(property));
                    var compatible = existing != null && existing.Type == item.Type;
                    var changedDefault = item.OriginalName != null && compatible
                        && ValueJson(existing.Value, item.Type) == ValueJson(old[item.OriginalName].DefaultValue, item.Type)
                        && !node.HasPropertyOverridesFor(item.OriginalName)
                        && !node.HasPersistedPropertyOverride(item.OriginalName);
                    var value = compatible && !changedDefault ? Copy(existing.Value, item.Type) : Copy(item.Property.DefaultValue, item.Type);
                    var baselineValue = compatible && baseline != null
                        && ValueJson(baseline.Value, item.Type) != ValueJson(old[item.OriginalName].DefaultValue, item.Type)
                        ? Copy(baseline.Value, item.Type) : Copy(item.Property.DefaultValue, item.Type);
                    var result = compatible ? new Property(existing) : new Property();
                    result.Name = item.Property.Name;
                    result.Type = item.Type;
                    result.Description = item.Property.Description;
                    result.Group = item.Property.Group;
                    result.DeclaringTypeName = declaringType;
                    result.FromComponent = declaring;
                    result.Value = value;
                    result.ReadOnly = false;
                    resultProperties.Add(result);
                    resultBaselines.Add(new Property(result) { Value = baselineValue });
                }

                var customs = node.FullName == declaringType
                    ? desired.Select(item => new CustomProperty(item.Property)
                        { DefaultValue = Copy(item.Property.DefaultValue, item.Type) }).ToList()
                    : node.CustomProperties;
                var removed = node.FullName == declaringType ? new List<string>(node.RemovedCustomProperties) : node.RemovedCustomProperties;
                if (node.FullName == declaringType)
                    foreach (var name in originalNames.Where(name => !mapping.TryGetValue(name, out var item) || item.Property.Name != name))
                        if (!removed.Contains(name))
                            removed.Add(name);
                Before.Add(Capture(node));
                After.Add(Capture(node, resultProperties, resultBaselines, customs, removed, unresolved, archive, persisted));
            }

            AffectedComponents = Before.Select(state => state.Component).ToList();
            var affected = AffectedComponents.ToHashSet();
            AffectedRoots = rootList.Where(root => Traverse(root).Any(affected.Contains)).ToList();
            PreservedValues = preserved;
        }

        /// <summary>Applies the prepared edits, rejecting stale component data.</summary>
        public void Apply()
        {
            if (Applied) throw new InvalidOperationException("The property edit is already applied.");
            Validate(Before);
            ValidateStored(After);
            foreach (var state in After) Restore(state);
            Applied = true;
        }

        /// <summary>Reverts the edits, rejecting changes made since they were applied.</summary>
        public void Undo()
        {
            if (!Applied) throw new InvalidOperationException("The property edit is not applied.");
            Validate(After);
            ValidateStored(Before);
            foreach (var state in Before) Restore(state);
            Applied = false;
        }

        private State Capture(Component node) => Capture(node, node.Properties, node.OveriddenProperties,
            node.CustomProperties, node.RemovedCustomProperties, node.UnresolvedPropertyOverrides,
            node.PreservedPropertyOverrides, node.PersistedPropertyOverrides);

        private State Capture(Component node, List<Property> properties, List<Property> baselines,
            List<CustomProperty> customs, List<string> removed, List<PropertyDTO> unresolved,
            List<PreservedPropertyDTO> preserved, HashSet<string> persisted) => new()
        {
            Component = node, Base = node.BaseInheritedComponent, FullName = node.FullName,
            Properties = properties, Baselines = baselines, Customs = customs, Removed = removed,
            Unresolved = unresolved, Preserved = preserved, Persisted = persisted,
            CustomJson = JsonSerializer.Serialize(customs, JSON.CreateOptions()),
            RemovedJson = JsonSerializer.Serialize(removed, JSON.CreateOptions()),
            UnresolvedJson = JsonSerializer.Serialize(unresolved, JSON.CreateOptions()),
            PreservedJson = JsonSerializer.Serialize(preserved, JSON.CreateOptions()),
            PersistedNames = persisted.ToArray(),
            PropertyStates = properties.Concat(baselines).Select(property => new PropertyState(property, new Property(property),
                ValueJson(property.Value, StoredType(property)))).ToList()
        };

        private void Validate(IEnumerable<State> states)
        {
            foreach (var state in states)
            {
                var node = state.Component;
                if (node.FullName != state.FullName || !ReferenceEquals(node.BaseInheritedComponent, state.Base)
                    || !ReferenceEquals(node.Properties, state.Properties) || !ReferenceEquals(node.OveriddenProperties, state.Baselines)
                    || !ReferenceEquals(node.CustomProperties, state.Customs) || !ReferenceEquals(node.RemovedCustomProperties, state.Removed)
                    || !ReferenceEquals(node.UnresolvedPropertyOverrides, state.Unresolved)
                    || !ReferenceEquals(node.PreservedPropertyOverrides, state.Preserved)
                    || !ReferenceEquals(node.PersistedPropertyOverrides, state.Persisted))
                    throw new InvalidOperationException($"Component '{node.FullName}' changed since the property edit was prepared.");
            }
            ValidateStored(states);
        }

        private void ValidateStored(IEnumerable<State> states)
        {
            foreach (var state in states)
                if (state.CustomJson != JsonSerializer.Serialize(state.Customs, JSON.CreateOptions())
                    || state.RemovedJson != JsonSerializer.Serialize(state.Removed, JSON.CreateOptions())
                    || state.UnresolvedJson != JsonSerializer.Serialize(state.Unresolved, JSON.CreateOptions())
                    || state.PreservedJson != JsonSerializer.Serialize(state.Preserved, JSON.CreateOptions())
                    || !state.Persisted.SetEquals(state.PersistedNames)
                    || !state.Properties.Concat(state.Baselines).SequenceEqual(state.PropertyStates.Select(item => item.Item))
                    || state.PropertyStates.Any(item => !Matches(item)))
                    throw new InvalidOperationException($"Component '{state.Component.FullName}' changed since the property edit was prepared.");
        }

        private bool Matches(PropertyState state)
        {
            var item = state.Item;
            var copy = state.Copy;
            return item.Name == copy.Name && item.Type == copy.Type && item.Description == copy.Description
                && item.Group == copy.Group && item.DeclaringTypeName == copy.DeclaringTypeName
                && ReferenceEquals(item.FromComponent, copy.FromComponent) && item.PropertyTemplateType == copy.PropertyTemplateType
                && item.ReadOnly == copy.ReadOnly && item.GenerateCode == copy.GenerateCode && item.Hidden == copy.Hidden
                && item.DynamicOptions == copy.DynamicOptions && ValueJson(item.Value, StoredType(item)) == state.Value;
        }

        private static void Restore(State state)
        {
            var node = state.Component;
            node.Properties = state.Properties;
            node.OveriddenProperties = state.Baselines;
            node.CustomProperties = state.Customs;
            node.RemovedCustomProperties = state.Removed;
            node.UnresolvedPropertyOverrides = state.Unresolved;
            node.PreservedPropertyOverrides = state.Preserved;
            node.PersistedPropertyOverrides = state.Persisted;
        }

        private Type StoredType(Property property)
        {
            if (property.Type == null) return property.Value?.GetType() ?? typeof(object);
            var definition = PropertyModel.Get(property.Type);
            return definition != null && definition.Type != definition.ValueType ? definition.ValueType : property.Type;
        }
        private static string ValueJson(object value, Type type) => JsonSerializer.Serialize(value, type, JSON.CreateOptions());
        private static object Copy(object value, Type type) =>
            JsonSerializer.SerializeToElement(value, type, JSON.CreateOptions()).Deserialize(type, JSON.CreateOptions());
        private PreservedPropertyDTO Archive(Property property, string declaringType) => new()
        {
            Name = property.Name, TypeName = StoredType(property).FullName, DeclaringTypeName = declaringType,
            Value = JsonSerializer.SerializeToElement(property.Value, StoredType(property), JSON.CreateOptions())
        };
        private static Component FindDeclaring(Component component, string name)
        {
            var seen = new HashSet<Component>();
            for (var current = component; current != null; current = current.BaseInheritedComponent)
            {
                if (!seen.Add(current)) throw new InvalidOperationException("The component inheritance chain contains a cycle.");
                if (current.FullName == name) return current;
            }
            return null;
        }
        private static IEnumerable<Component> Traverse(Component root)
        {
            var seen = new HashSet<Component>();
            var pending = new Stack<Component>();
            pending.Push(root);
            while (pending.TryPop(out var node))
            {
                if (!seen.Add(node)) continue;
                yield return node;
                if (node.BaseInheritedComponent != null) pending.Push(node.BaseInheritedComponent);
                foreach (var child in node.Children) pending.Push(child);
            }
        }
    }
}
