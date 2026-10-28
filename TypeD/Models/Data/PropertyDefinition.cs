using System;
using System.Text.Json.Serialization;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using TypeD.ViewModel;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Describes everything the editor needs to know to handle a property of a specific type: how it is written to
    /// JSON, how it is written as code, and how it is edited in the UI.
    /// </summary>
    public interface IPropertyDefinition
    {
        /// <summary>
        /// The property value type this definition handles.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Converter used when saving and loading values of <see cref="Type"/> as JSON, or <see langword="null"/> to
        /// use the default System.Text.Json handling.
        /// </summary>
        JsonConverter JsonConverter { get; }

        /// <summary>
        /// Creates the C# expression that evaluates to the value, used when generating component code.
        /// </summary>
        /// <param name="value">A non-null value of <see cref="Type"/>.</param>
        string ToCode(object value);

        /// <summary>
        /// Creates the view model used to edit the property in the properties panel.
        /// </summary>
        PropertyNode CreateNode(Component component, Property property, Action<PropertyNode, object> valueChanged);

        /// <summary>
        /// Creates the template used to edit the property. Its data context is the node from <see cref="CreateNode"/>.
        /// </summary>
        IDataTemplate CreateTemplate();
    }

    /// <summary>
    /// Base class for property definitions.
    /// </summary>
    /// <typeparam name="TValue">The property value type.</typeparam>
    /// <typeparam name="TNode">The view model type. Needs a constructor taking (Component, Property, Action&lt;PropertyNode, object&gt;).</typeparam>
    public abstract class PropertyDefinition<TValue, TNode> : IPropertyDefinition where TNode : PropertyNode
    {
        public Type Type => typeof(TValue);

        public virtual JsonConverter JsonConverter => null;

        /// <summary>
        /// Location of the resource dictionary containing the template, e.g. avares://MyModule/Templates/MyTemplate.axaml
        /// </summary>
        protected abstract string TemplateUri { get; }

        /// <summary>
        /// Key of the <see cref="IDataTemplate"/> in the resource dictionary at <see cref="TemplateUri"/>.
        /// </summary>
        protected abstract string TemplateKey { get; }

        public abstract string ToCode(TValue value);

        string IPropertyDefinition.ToCode(object value) => ToCode((TValue)value);

        public virtual PropertyNode CreateNode(Component component, Property property, Action<PropertyNode, object> valueChanged)
        {
            return (PropertyNode)Activator.CreateInstance(typeof(TNode), component, property, valueChanged);
        }

        public virtual IDataTemplate CreateTemplate()
        {
            var resources = (IResourceDictionary)AvaloniaXamlLoader.Load(new Uri(TemplateUri));
            if (resources.TryGetResource(TemplateKey, null, out var template) && template is IDataTemplate dataTemplate)
                return dataTemplate;
            return null;
        }
    }
}
