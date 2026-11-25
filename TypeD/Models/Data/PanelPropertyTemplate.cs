using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Node of a property that is edited in the property editor panel. The properties panel shows a summary and an edit button.
    /// </summary>
    public class PanelPropertyNode : PropertyNode
    {
        private readonly PanelPropertyTemplate _template;

        /// <summary>Opens the property editor panel for this property.</summary>
        public ICommand EditCommand { get; }

        /// <summary>Gets the short text shown next to the edit button.</summary>
        public string Summary => _template.GetSummary(Property.Value);

        /// <summary>Creates a node for a panel edited property.</summary>
        public PanelPropertyNode(Component component, Property property, Action<PropertyNode, object> value_changed, PanelPropertyTemplate template) : base(component, property, value_changed)
        {
            _template = template;
            EditCommand = new RelayCommand(() => ResourceModel?.Get<IPropertyEditorModel>().Open(this, _template), () => !ReadOnly);
        }

        /// <inheritdoc/>
        public override void RefreshValue()
        {
            base.RefreshValue();
            OnPropertyChanged(nameof(Summary));
        }
    }

    /// <summary>
    /// Base class for property templates that edit a property in its own panel instead of inline. A module derives
    /// from it, creates the editor control, and may attach extra viewer overlays while a session is open
    /// (see <see cref="Hooks.PropertyEditorOpenedHook"/>). Select it with <see cref="Property.PropertyTemplateType"/>
    /// or a <see cref="IPropertyDefinition.PropertyTemplateType"/>, and register it with <see cref="IPropertyModel.RegisterTemplate"/>.
    /// </summary>
    public abstract class PanelPropertyTemplate : PropertyTemplate
    {
        /// <inheritdoc/>
        protected override string TemplateUri => "avares://TypeDitor/View/Panels/PropertyTemplates/PanelPropertyTemplate.axaml";
        /// <inheritdoc/>
        protected override string TemplateKey => "PanelPropertyTemplate";

        /// <summary>Gets the title shown at the top of the property editor panel.</summary>
        public virtual string EditorTitle => "Edit property";

        /// <summary>Creates the control shown in the property editor panel for the session.</summary>
        public abstract Control CreateEditor(PropertyEditorSession session);

        /// <summary>Gets the short text shown in the properties panel for a value.</summary>
        public virtual string GetSummary(object value) => value?.ToString() ?? string.Empty;

        /// <inheritdoc/>
        public override PropertyNode CreateNode(Component component, Property property, Action<PropertyNode, object> valueChanged)
        {
            return new PanelPropertyNode(component, property, valueChanged, this);
        }
    }
}
