using System;
using TypeD.Models.Data;

namespace TypeD.ViewModel
{
    /// <summary>
    /// View model for a single property shown in the properties panel. Derive from it to adapt a property value
    /// to what a <see cref="Models.Data.IPropertyDefinition"/> template binds to.
    /// </summary>
    public class PropertyNode : ViewModelBase
    {
        public Component Component { get; private set; }
        public Property Property { get; private set; }

        public string Name { get => Property.Name; }
        public virtual object Value
        {
            get
            {
                return Property.Value;
            }
            set
            {
                if (!Equals(Property.Value, value))
                {
                    ValueChanged(this, value);
                }
            }
        }

        public Type Type { get => Property.Type; }
        public string Description { get => Property.Description; }
        public bool ReadOnly { get => Property.ReadOnly; }
        public Action<PropertyNode, object> ValueChanged;

        public PropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed)
        {
            Component = component;
            Property = property;
            ValueChanged = value_changed;
        }

        public virtual void RefreshValue()
        {
            OnPropertyChanged(nameof(Value));
        }
    }
}
