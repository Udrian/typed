using TypeD.Models.Data;
using TypeD.ViewModel;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Hosts the property editor panel used by <see cref="PanelPropertyTemplate"/> properties. Only one session is open at a time.
    /// </summary>
    public interface IPropertyEditorModel : IModel
    {
        /// <summary>The id of the property editor panel.</summary>
        public const string PanelID = "typed_property_editor";

        /// <summary>Gets the open session, or <see langword="null"/>.</summary>
        public PropertyEditorSession Current { get; }

        /// <summary>
        /// Opens the property editor for a named property of a component, using the property's panel template.
        /// Safe to call from any thread; the panel is opened on the UI thread. Returns <see langword="false"/> when
        /// the property does not exist or is not edited in a panel.
        /// </summary>
        public bool OpenProperty(Project project, Component component, string propertyName);

        /// <summary>Opens the property editor panel for a property node, closing any previous session.</summary>
        public PropertyEditorSession Open(PropertyNode node, PanelPropertyTemplate template);

        /// <summary>Ends the open session. The panel stays open and shows that nothing is being edited.</summary>
        public void Close();
    }
}
