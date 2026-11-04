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
        private string _validationError;

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
        /// <summary>Gets the editor group of the property.</summary>
        public string Group => Property.Group;
        /// <summary>Matches a filter against the property name, group, and current value, ignoring case.</summary>
        public bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var query = filter.Trim();
            return Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
                || Group.Contains(query, StringComparison.OrdinalIgnoreCase)
                || Property.Value?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
        }
        /// <summary>Gets the description, declaring class, and any validation error shown on the property name.</summary>
        public string NameToolTip
        {
            get
            {
                var declaringType = Property.DeclaringTypeName ?? Property.FromComponent?.FullName;
                var text = Description;
                if (!string.IsNullOrEmpty(declaringType))
                    text = string.IsNullOrEmpty(text) ? $"Declared by: {declaringType}" : $"{text}\n\nDeclared by: {declaringType}";
                if (HasValidationError)
                    text = string.IsNullOrEmpty(text) ? ValidationError : $"{text}\n\n{ValidationError}";
                return text;
            }
        }
        /// <summary>Gets a value indicating whether the property or its component is read-only.</summary>
        public bool ReadOnly { get => Property.ReadOnly || Component?.ReadOnly == true; }
        /// <summary>Gets or sets the validation error message for this property. Set to null or empty string to clear errors.</summary>
        public string ValidationError
        {
            get => _validationError;
            set
            {
                if (_validationError != value)
                {
                    _validationError = value;
                    OnPropertyChanged(nameof(ValidationError));
                    OnPropertyChanged(nameof(HasValidationError));
                    OnPropertyChanged(nameof(NameToolTip));
                }
            }
        }
        /// <summary>Gets whether this property has a validation error.</summary>
        public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);
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