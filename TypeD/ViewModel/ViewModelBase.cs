using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TypeD.Models.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TypeD.ViewModel
{
    /// <summary>Base class of view models; it attaches to the UI notify model so elements can be added and removed.</summary>
    public class ViewModelBase : ObservableObject
    {
        // Properties
        /// <summary>Gets or sets the application's main window.</summary>
        public static Window MainWindow {
            get { return (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow; }
            set { (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow = value; }
        }

        // Models
        /// <summary>Gets the resource model used to get models and providers.</summary>
        public static IResourceModel ResourceModel { get; internal set; }
        /// <summary>Gets the UI notify model this view model is attached to, or <c>null</c> if it has no element.</summary>
        public IUINotifyModel UINotifyModel { get; private set; }

        // Constructors
        /// <summary>Initializes a new instance of the <see cref="ViewModelBase"/> class.</summary>
        /// <param name="element">The view element; when given, the view model attaches to the UI notify model.</param>
        public ViewModelBase(Control element = null)
        {
            if(element != null)
            {
                UINotifyModel = ResourceModel.Get<IUINotifyModel>();
                UINotifyModel.Attach(GetType().FullName, (name) => {
                },
                (element, remove) =>
                {
                    if(!remove)
                        OnAddElement(element);
                    else
                        OnRemoveElement(element);
                });
            }
        }

        // Functions
        /// <summary>Called when an element is added to this view model.</summary>
        /// <param name="element">The added element.</param>
        public virtual void OnAddElement(object element)
        {
        }
        /// <summary>Called when an element is removed from this view model.</summary>
        /// <param name="element">The removed element.</param>
        public virtual void OnRemoveElement(object element)
        {
        }
    }
}
