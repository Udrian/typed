using System;
using TypeD.Code;
using TypeD.Helpers;
using TypeD.Models.Data;

namespace TypeD.Components
{
    public abstract class ComponentTemplate
    {
        // Properties
        public ComponentTypeCode Code { get; internal set; }
        public Component Component { get; internal set; }

        // Constructors
        internal ComponentTemplate() { }
        internal abstract void CreateCode(Component component);
        public abstract void Init();

        // Functions
        public abstract void ChildrenFilter(FilterHelper filter);
    }

    public abstract class ComponentTemplate<T> : ComponentTemplate where T : ComponentTypeCode
    {
        // Properties
        public new T Code { get { return base.Code as T; } internal set { base.Code = value; } }

        // functions
        internal override void CreateCode(Component component)
        {
            Component = component;
            Code = Activator.CreateInstance(typeof(T), component) as T;
        }
    }
}
