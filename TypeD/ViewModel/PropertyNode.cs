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
        /// <summary>Gets the component the property belongs to.</summary>
        public Component Component { get; private set; }
        /// <summary>Gets the property shown by the node.</summary>
        public Property Property { get; private set; }

        /// <summary>Gets the property name.</summary>
        public string Name { get => Property.Name; }
        /// <summary>Gets or sets the property value; setting a different value calls <see cref="ValueChanged"/>.</summary>
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

        /// <summary>Gets the property type.</summary>
        public Type Type { get => Property.Type; }
        /// <summary>Gets the property description.</summary>
        public string Description { get => Property.Description; }
        /// <summary>Gets a value indicating whether the property or its component is read-only.</summary>
        public bool ReadOnly { get => Property.ReadOnly || Component.ReadOnly; }
        /// <summary>Called with this node and the new value when the user changes the value.</summary>
        public Action<PropertyNode, object> ValueChanged;

        /// <summary>Initializes a new instance of the <see cref="PropertyNode"/> class.</summary>
        /// <param name="component">The component the property belongs to.</param>
        /// <param name="property">The property to show.</param>
        /// <param name="value_changed">Called with this node and the new value when the user changes the value.</param>
        public PropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed)
        {
            Component = component;
            Property = property;
            ValueChanged = value_changed;
        }

        /// <summary>Notifies the view that <see cref="Value"/> changed so it is read again.</summary>
        public virtual void RefreshValue()
        {
            OnPropertyChanged(nameof(Value));
        }
    }
}
