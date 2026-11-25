using Avalonia.Controls;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Models
{
    internal sealed class PropertyEditorModel : IPropertyEditorModel
    {
        private IHookModel _hookModel;
        private IResourceModel _resourceModel;
        private IPanelModel _panelModel;
        private ContentControl _host;
        private Window _keyWindow;

        public PropertyEditorSession Current { get; private set; }

        public void Init(IResourceModel resourceModel)
        {
            _hookModel = resourceModel.Get<IHookModel>();
            _resourceModel = resourceModel;
            _panelModel = resourceModel.Get<IPanelModel>();

            _host = new ContentControl { Content = CreatePlaceholder() };
            _panelModel.AttachPanel(IPropertyEditorModel.PanelID, "Property Editor", _host, Dock.Model.Core.Alignment.Right);

            _hookModel.AddHook<PropertyChangedHook>(OnPropertyChanged);
            _hookModel.AddHook<ComponentFocusHook>(hook =>
            {
                if (Current != null && !IsSame(hook.Component, Current.Component))
                    Close();
            });
            _hookModel.AddHook<CloseComponentHook>(hook =>
            {
                if (Current != null && IsSame(hook.Component, Current.Component))
                    Close();
            });
            _hookModel.AddHook<PanelStatusChangedHook>(hook =>
            {
                if (Current != null && hook.Panel?.ID == IPropertyEditorModel.PanelID
                    && (hook.Status == PanelStatusChangedHook.PanelStatus.Closed || hook.Status == PanelStatusChangedHook.PanelStatus.ClosedByUser))
                    EndSession();
            });
        }

        public bool OpenProperty(Project project, Component component, string propertyName)
        {
            var property = component?.Properties.FirstOrDefault(p => p.Name == propertyName);
            if (property == null)
                return false;

            var propertyModel = _resourceModel.Get<IPropertyModel>();
            var definition = propertyModel.Get(property.Type);
            var templateType = property.PropertyTemplateType ?? definition?.PropertyTemplateType ?? property.Type;
            if (propertyModel.GetTemplate(templateType) is not PanelPropertyTemplate template)
                            return false;

            var registry = _resourceModel.Get<IEditorCommandRegistry>();
            var node = template.CreateNode(component, property, (n, value) =>
            {
                if (!registry.Execute("component.change-property", new ChangePropertyCommandData { Project = project, Property = n.Property, Value = value }))
                    throw new InvalidOperationException($"Could not execute the change-property command for '{n.Property.Name}'.");
            });
            // Callers such as the viewer run on the game thread, while the panel must be built on the UI thread
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                Open(node, template);
            else
                Avalonia.Threading.Dispatcher.UIThread.Post(() => Open(node, template));
            return true;
        }

        public PropertyEditorSession Open(PropertyNode node, PanelPropertyTemplate template)
        {
            if (Current != null)
                EndSession();

            var session = new PropertyEditorSession(node.Component, node.Property, node, template);
            session.CloseRequested = Close;
            Current = session;

            var title = new TextBlock
            {
                Text = $"{template.EditorTitle} - {node.Component.Name}.{node.Name}",
                Margin = new Avalonia.Thickness(8, 6),
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var exit = new Button { Content = "Exit editing", Margin = new Avalonia.Thickness(8, 4), Focusable = false };
            exit.Click += (_, _) => Close();
            DockPanel.SetDock(exit, Avalonia.Controls.Dock.Right);
            var header = new DockPanel { Children = { exit, title } };
            DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);
            _host.Content = new DockPanel
            {
                LastChildFill = true,
                Children = { header, template.CreateEditor(session) }
            };

            _panelModel.OpenPanel(IPropertyEditorModel.PanelID);
            AttachKeyHandler();
            _hookModel.Shoot(new PropertyEditorOpenedHook { Session = session });
            return session;
        }

        // Esc exits editing from anywhere; Enter does too, except in a text box where it commits the typed value
        private void OnKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            if (Current == null || e.KeyModifiers != Avalonia.Input.KeyModifiers.None)
                return;
            var inTextBox = (sender as TopLevel)?.FocusManager?.GetFocusedElement() is TextBox;
            if (e.Key == Avalonia.Input.Key.Escape || (e.Key == Avalonia.Input.Key.Enter && !inTextBox))
            {
                e.Handled = true;
                Close();
            }
        }

        private void AttachKeyHandler()
        {
            var window = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (window == null || ReferenceEquals(window, _keyWindow))
                return;
            DetachKeyHandler();
            _keyWindow = window;
            window.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        private void DetachKeyHandler()
        {
            _keyWindow?.RemoveHandler(Avalonia.Input.InputElement.KeyDownEvent, OnKeyDown);
            _keyWindow = null;
        }

        public void Close()
        {
            if (Current == null)
                return;
            EndSession();
        }

        private static TextBlock CreatePlaceholder()
        {
            return new TextBlock
            {
                Text = "Not editing a property",
                Opacity = 0.6,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
        }

        private void EndSession()
        {
            var session = Current;
            if (session == null)
                return;
            Current = null;
            DetachKeyHandler();
            session.IsOpen = false;
            _host.Content = CreatePlaceholder();
            session.RaiseClosed();
            _hookModel.Shoot(new PropertyEditorClosedHook { Session = session });
        }

        private void OnPropertyChanged(PropertyChangedHook hook)
        {
            var session = Current;
            if (session == null || hook.Property == null || !IsSame(hook.Component, session.Component))
                return;
            if (hook.Property.Name != session.Property.Name)
            {
                // Editors may depend on sibling properties of the same component
                session.RaiseChanged();
                return;
            }
            session.Property = hook.Property;
            session.Node.RefreshValue();
            session.RaiseChanged();
        }

        private static bool IsSame(Component a, Component b)
        {
            if (a == null || b == null)
                return false;
            return ReferenceEquals(a, b) || (a.ID == b.ID && a.FullParentPath == b.FullParentPath);
        }
    }
}
