using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using TypeD.ViewModel;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Defines a reusable editor template that can be selected by a property or setting independently of its value type.
    /// </summary>
    public abstract class PropertyTemplate
    {
        /// <summary>Gets the type used to associate this template with a property or setting.</summary>
        public Type Type => GetType();

        /// <summary>Gets the resource dictionary containing the template.</summary>
        protected abstract string TemplateUri { get; }

        /// <summary>Gets the resource key for the template in <see cref="TemplateUri"/>.</summary>
        protected abstract string TemplateKey { get; }

        /// <summary>Creates a specialized property node, or returns <see langword="null"/> to use the property's definition.</summary>
        public virtual PropertyNode CreateNode(Component component, Property property, Action<PropertyNode, object> valueChanged)
        {
            return null;
        }

        /// <summary>Loads the registered Avalonia data template.</summary>
        public virtual IDataTemplate CreateTemplate()
        {
            var resources = (IResourceDictionary)AvaloniaXamlLoader.Load(new Uri(TemplateUri));
            if (resources.TryGetResource(TemplateKey, null, out var template) && template is IDataTemplate dataTemplate)
                return dataTemplate;
            throw new InvalidOperationException($"Property template '{TemplateKey}' was not found in '{TemplateUri}'.");
        }
    }
}
