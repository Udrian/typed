using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TypeD.Models.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TypeD.ViewModel
{
    public class ViewModelBase : ObservableObject
    {
        // Properties
        public static Window MainWindow {
            get { return (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow; }
            set { (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow = value; }
        }

        // Models
        public static IResourceModel ResourceModel { get; internal set; }
        public IUINotifyModel UINotifyModel { get; private set; }

        // Constructors
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
        public virtual void OnAddElement(object element)
        {
        }
        public virtual void OnRemoveElement(object element)
        {
        }
    }
}
