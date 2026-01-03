using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TypeD.Models.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Metadata;

namespace TypeD.ViewModel
{
    public class ViewModelBase : ObservableObject
    {
        // Properties
        public static Window MainWindow {
            get { return (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow; }
            set { (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow = value; }
        }
        public Control ParentControl { get; private set; }

        // Models
        public IResourceModel ResourceModel { get; private set; }
        public IUINotifyModel UINotifyModel { get; private set; }

        public static IResourceModel resourceModelStatic;

        // Constructors
        public ViewModelBase(Control element = null)
        {
            if(element != null)
            {
                ParentControl = element;

                if(element.TryFindResource("ResourceModel", out object model))
                {
                    ResourceModel = model as IResourceModel;
                }
                //TODO: Ugly hack, don't use this
                if(resourceModelStatic == null && ResourceModel != null)
                {
                    resourceModelStatic = ResourceModel;
                }
                if(ResourceModel == null && resourceModelStatic != null)
                {
                    ResourceModel = resourceModelStatic;
                }

                UINotifyModel = ResourceModel.Get<IUINotifyModel>();
                UINotifyModel.Attach(GetType().FullName, (name) => {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        public virtual void OnAddElement(object element)
        {
        }
        public virtual void OnRemoveElement(object element)
        {
        }

        // Events
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
