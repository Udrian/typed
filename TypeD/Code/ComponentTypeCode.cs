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
        public abstract Type TypeOBaseType { get; } // TODO: Remove this.
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
        }

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
            AddChildrenCode(Component, "this", false, ref childIndex);
            if (Component.Children.Count > 0)
                Writer.NewLine();
        }

        private void AddChildrenCode(Component parent, string parentExpression, bool instanceChildrenOnly, ref int childIndex)
        {
            foreach (var childComponent in parent.Children)
            {
                var definitionChild = instanceChildrenOnly && parent.DefinitionChildren.Contains(childComponent);
                if (definitionChild && !childComponent.HasChanges)
                    continue;

                var isEntity = childComponent.TypeOBaseType.FullName == "TypeOEngine.Typedeaf.Core.Entities.Entity";
                var isDrawable = childComponent.TypeOBaseType.FullName == "TypeOEngine.Typedeaf.Core.Entities.Drawables.Drawable";
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
                    AddChildrenCode(childComponent, variableName, true, ref childIndex);
                }

                Writer.AddRightCurlyBrackets();
            }
        }

        protected void AddPropertyCode(TypeD.Models.Data.Property property, string prepend = "")
        {
            if (string.IsNullOrEmpty(property.Name) || property.ReadOnly)
                return;

            var valuestring = "";
            var definition = ViewModelBase.ResourceModel?.Get<IPropertyModel>()?.Get(property.Type);
            if (property.Value == null)
            {
                valuestring = "null";
            }
            else if (definition != null)
            {
                valuestring = definition.ToCode(property.Value);
            }
            else if (property.Type == typeof(bool))
            {
                valuestring = ((bool)property.Value) ? "true" : "false";
            }
            else if(property.Type == typeof(string))
            {
                valuestring = $"\"{(string.IsNullOrEmpty((string)property.Value) ? "" : (string)property.Value)}\"";
            }
            else if (property.Type.IsPrimitive)
            {
                valuestring = property.Value.ToString();
            }
            else if (property.Type.IsClass || property.Type.IsValueType)
            {
                List<string> initList = new List<string>();
                foreach (var initProperty in property.Type.GetProperties())
                {
                    if (initProperty.CanRead && initProperty.CanWrite && initProperty.GetSetMethod(true).IsPublic)
                    {
                        var value = initProperty.GetValue(property.Value);
                        initList.Add($"{initProperty.Name} = {value}");
                    }
                }

                valuestring = $"new {property.Type.FullName}()";
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
                valuestring = property.Value.ToString();
            }
            Writer.AddLine($"{prepend}{property.Name} = {valuestring};");
        }
    }
}
