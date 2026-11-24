using TypeD.ViewModel;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Exposes the value of a <see cref="Property"/> and the options computed by its
    /// <see cref="Property.DynamicOptions"/> to a combo box.
    /// </summary>
    public class DynamicComboBoxPropertyNode : PropertyNode
    {
        /// <summary>Creates a node for a property with dynamic options.</summary>
        public DynamicComboBoxPropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed) : base(component, property, value_changed) { }

        /// <summary>Gets the options computed from the current state of the component.</summary>
        public IReadOnlyList<string> Options => Property.DynamicOptions?.Invoke(Component)?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Editor template that shows a combo box built from <see cref="Property.DynamicOptions"/>. Select it with
    /// <see cref="Property.PropertyTemplateType"/>, or use <see cref="Property.CreateDynamicComboBox"/>.
    /// </summary>
    public sealed class DynamicComboBoxPropertyTemplate : PropertyTemplate
    {
        /// <inheritdoc/>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/DynamicComboBoxPropertyTemplate.axaml";
        /// <inheritdoc/>
        protected override string TemplateKey => "DynamicComboBoxPropertyTemplate";

        /// <inheritdoc/>
        public override PropertyNode CreateNode(Component component, Property property, Action<PropertyNode, object> valueChanged)
        {
            return new DynamicComboBoxPropertyNode(component, property, valueChanged);
        }
    }
}
