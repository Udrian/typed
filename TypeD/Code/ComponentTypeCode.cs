using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Code
{
    /// <summary>
    /// Represents an abstract base class for defining and analyzing component type codes.
    /// </summary>
    /// <remarks>This class provides a framework for working with component type codes, including properties 
    /// to access the base type, determine if the component is a base type, and retrieve associated components. It is
    /// designed to be extended by derived classes that implement specific behavior for component type
    /// analysis.</remarks>
    public abstract class ComponentTypeCode : TypeDCodalyzer
    {
        // Properties
        /// <summary>
        /// Gets the base type of the current type.
        /// </summary>
        public abstract Type TypeOBaseType { get; }
        /// <summary>
        /// Gets a value indicating whether the current type is the base component type.
        /// </summary>
        public virtual bool IsBaseComponentType
        {
            get
            {
                return BaseClass == TypeOBaseType.FullName;
            }
        }

        /// <summary>
        /// Gets the component associated with the current instance.
        /// </summary>
        public Component Component { get; private set; }

        /// <summary>
        /// Gets the base inherited component of the current component.
        /// </summary>
        public Component BaseInheritedComponent { get { return Component.BaseInheritedComponent; } }

        // Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentTypeCode"/> class using the specified component.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> instance used to initialize this object. Must not be <see langword="null"/>.</param>
        public ComponentTypeCode(Component component)
        {
            ClassName = component.ClassName;
            Namespace = component.Namespace;
            Component = component;
            BaseClass = BaseInheritedComponent == null ? TypeOBaseType.FullName : BaseInheritedComponent.FullName;
            TypeDFile.DynamicProperties = DynamicTypeDProperties;
        }

        private static readonly string[] ReservedNames =
        {
            "ID", "Name", "Pause", "Hidden", "DrawOrder", "Parent", "Entities", "Drawables", "Game", "Scene", "Context",
            "Initialize", "Update", "Draw", "Remove", "Destroy", "Position", "Size", "Origin", "Rotation", "Scale",
            "FullParentPath", "Visible", "Enabled"
        };

        private Dictionary<Component, string> GetPublicChildNames()
        {
            var names = new Dictionary<Component, string>();
            var used = new HashSet<string>(ReservedNames) { Component.ClassName };
            for (var declaring = Component; declaring != null; declaring = declaring.BaseInheritedComponent)
            {
                foreach (var customProperty in declaring.CustomProperties)
                    used.Add(customProperty.Name);
            }
            CollectPublicChildren(Component, false, names, used);
            return names;
        }

        private void CollectPublicChildren(Component parent, bool instanceChildrenOnly, Dictionary<Component, string> names, HashSet<string> used)
        {
            foreach (var child in parent.Children)
            {
                var definitionChild = instanceChildrenOnly && parent.DefinitionChildren.Contains(child);
                if (definitionChild && (!child.HasChanges || child.Removed))
                    continue;

                var baseTypeName = child.RootBaseComponent.FullName;
                var isEntity = baseTypeName == "TypeOEngine.Typedeaf.Core.Entities.Entity";
                var isDrawable = baseTypeName == "TypeOEngine.Typedeaf.Core.Entities.Drawables.Drawable";
                if (!isEntity && !isDrawable)
                    continue;

                if (!definitionChild && child.Public)
                {
                    var name = CSharpIdentifier.Sanitize(child.Name);
                    var candidate = name;
                    for (var i = 2; !used.Add(candidate); i++)
                        candidate = name + i;
                    names[child] = candidate;
                }

                if (isEntity)
                    CollectPublicChildren(child, true, names, used);
            }
        }

        private List<Codalyzer.Property> PublicChildProperties()
        {
            return GetPublicChildNames().Select(pair => new Codalyzer.Property(
                $"public {pair.Key.FullName} {pair.Value}",
                writer =>
                {
                    writer.AddLine("get;");
                    writer.AddLine("private set;");
                })).ToList();
        }

        private List<Codalyzer.Property> DynamicTypeDProperties()
        {
            var properties = PublicChildProperties();
            properties.AddRange(CustomPropertyCodes());
            return properties;
        }

        /// <summary>Creates the generated C# properties for the user defined properties of the component.</summary>
        private List<Codalyzer.Property> CustomPropertyCodes()
        {
            var properties = new List<Codalyzer.Property>();
            foreach (var customProperty in Component.CustomProperties)
            {
                if (string.IsNullOrEmpty(customProperty.Name))
                    continue;

                var type = ResolveType(customProperty.TypeName);
                var typeName = type != null ? GetTypeCodeName(type) : customProperty.TypeName;
                var access = customProperty.Access.ToString().ToLowerInvariant();
                var group = EscapeString(customProperty.Group ?? "General");
                var arguments = string.IsNullOrEmpty(customProperty.Description)
                    ? $"Group = \"{group}\""
                    : $"\"{EscapeString(customProperty.Description)}\", Group = \"{group}\"";
                properties.Add(new Codalyzer.Property($"{access} {typeName} {customProperty.Name}")
                {
                    Attributes = new List<string>
                    {
                        $"[TypeOEngine.Typedeaf.Core.Attributes.TypeOProperty({arguments})]"
                    },
                    Initializer = customProperty.DefaultValue == null || type == null
                        ? null
                        : GetValueCode(type, customProperty.DefaultValue)
                });
            }
            return properties;
        }

        private static string EscapeString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
            return Type.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == typeName);
        }

        private static string GetTypeCodeName(Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(string)) return "string";
            return type.FullName;
        }

        /// <summary>Writes the code that sets the component's properties and creates its children.</summary>
        protected void TypeDInitializeCode()
        {
            if (Component.Properties.Count > 0)
                Writer.AddLine("//Properties");
            foreach (var property in Component.Properties)
            {
                AddPropertyCode(property);
            }
            
            if(Component.Properties.Count > 0)
                Writer.NewLine();
            if (Component.Children.Count > 0)
                Writer.AddLine("//Children");
            var childIndex = 0;
            var publicNames = GetPublicChildNames();
            AddChildrenCode(Component, "this", false, ref childIndex, publicNames);
            if (Component.Children.Count > 0)
                Writer.NewLine();
        }

        private void AddChildrenCode(Component parent, string parentExpression, bool instanceChildrenOnly, ref int childIndex, Dictionary<Component, string> publicNames)
        {
            foreach (var childComponent in parent.Children)
            {
                var definitionChild = instanceChildrenOnly && parent.DefinitionChildren.Contains(childComponent);
                if (definitionChild && !childComponent.HasChanges)
                    continue;

                var isEntity = childComponent.RootBaseComponent.FullName == "TypeOEngine.Typedeaf.Core.Entities.Entity";
                var isDrawable = childComponent.RootBaseComponent.FullName == "TypeOEngine.Typedeaf.Core.Entities.Drawables.Drawable";
                if (!isEntity && !isDrawable)
                {
                    continue;
                }

                var variableName = $"child{childIndex++}";
                var collection = isEntity ? $"{parentExpression}.Entities" : $"{parentExpression}.Drawables";
                Writer.AddLeftCurlyBracket();
                if (definitionChild)
                {
                    var accessor = isEntity ? "GetEntityByID" : "GetDrawableByID";
                    Writer.AddLine($"var {variableName} = {collection}.{accessor}<{childComponent.FullName}>(({parentExpression} as TypeOEngine.Typedeaf.Core.Engine.Interfaces.IHasParent).FullParentPath + {parentExpression}.ID + \"/{childComponent.ID}\");");
                    if (childComponent.Removed)
                    {
                        Writer.AddLine(isEntity
                            ? $"{variableName}?.Remove();"
                            : $"if ({variableName} != null) {collection}.Destroy({variableName});");
                        Writer.AddRightCurlyBrackets();
                        continue;
                    }
                }
                else
                {
                    Writer.AddLine($"var {variableName} = {collection}.Create<{childComponent.FullName}>(\"{childComponent.ID}\");");
                    if (publicNames.TryGetValue(childComponent, out var publicName))
                        Writer.AddLine($"{publicName} = {variableName};");
                }

                foreach (var property in childComponent.Properties)
                {
                    if (childComponent.HasPropertyOverridesFor(property.Name))
                    {
                        AddPropertyCode(property, $"{variableName}.");
                    }
                }

                if (isEntity)
                {
                    AddChildrenCode(childComponent, variableName, true, ref childIndex, publicNames);
                }

                Writer.AddRightCurlyBrackets();
            }
        }

        /// <summary>Writes the code that assigns a property's value; read-only and unnamed properties are skipped.</summary>
        /// <param name="property">The property to write.</param>
        /// <param name="prepend">Text placed before the property name, e.g. a child variable and a dot.</param>
        protected void AddPropertyCode(TypeD.Models.Data.Property property, string prepend = "")
        {
            if (string.IsNullOrEmpty(property.Name) || property.ReadOnly)
                return;

            Writer.AddLine($"{prepend}{property.Name} = {GetValueCode(property.Type, property.Value)};");
        }

        /// <summary>Creates the C# expression that evaluates to the given value of the given type.</summary>
        /// <param name="type">The type of the value.</param>
        /// <param name="value">The value to convert to code.</param>
        protected string GetValueCode(Type type, object value)
        {
            var valuestring = "";
            var definition = ViewModelBase.ResourceModel?.Get<IPropertyModel>()?.Get(type);
            if (value == null)
            {
                valuestring = "null";
            }
            else if (definition != null)
            {
                valuestring = definition.ToCode(value);
            }
            else if (type == typeof(bool))
            {
                valuestring = ((bool)value) ? "true" : "false";
            }
            else if(type == typeof(string))
            {
                valuestring = $"\"{(string.IsNullOrEmpty((string)value) ? "" : (string)value)}\"";
            }
            else if (type.IsPrimitive)
            {
                valuestring = value.ToString();
            }
            else if (type.IsClass || type.IsValueType)
            {
                List<string> initList = new List<string>();
                foreach (var initProperty in type.GetProperties())
                {
                    if (initProperty.CanRead && initProperty.CanWrite && initProperty.GetSetMethod(true).IsPublic)
                    {
                        var initValue = initProperty.GetValue(value);
                        initList.Add($"{initProperty.Name} = {initValue}");
                    }
                }

                valuestring = $"new {type.FullName}()";
                if (initList.Count > 0) valuestring += "{ ";
                var first = true;
                foreach (var initLine in initList)
                {
                    valuestring += $"{(first ? "" : ", ")}{initLine}";
                    first = false;
                }
                if (initList.Count > 0) valuestring += " }";
            }
            else
            {
                valuestring = value.ToString();
            }
            return valuestring;
        }
    }
}
