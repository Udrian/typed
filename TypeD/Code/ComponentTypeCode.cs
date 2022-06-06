using System;
using TypeD.Models.Data;

namespace TypeD.Code
{
    public abstract class ComponentTypeCode : TypeDCodalyzer
    {
        // Properties
        public abstract Type TypeOBaseType { get; }
        public bool IsBaseComponentType
        {
            get
            {
                return BaseClass == TypeOBaseType.FullName;
            }
        }

        public Component Component { get; private set; }

        public Component ParentComponent { get { return Component.ParentComponent; } }

        // Constructors
        public ComponentTypeCode(Component component)
        {
            ClassName = component.ClassName;
            Namespace = component.Namespace;
            Component = component;
            BaseClass = ParentComponent == null ? TypeOBaseType.FullName : ParentComponent.FullName;
        }
    }
}
