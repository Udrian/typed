using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TypeD.Models.Interfaces;

namespace TypeD.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        // Models
        public IResourceModel ResourceModel { get; private set; }
        public IUINotifyModel UINotifyModel { get; private set; }

        // Constructors
        public ViewModelBase(FrameworkElement element = null)
        {
            if(element != null)
            {
                ResourceModel = element.FindResource("ResourceModel") as IResourceModel;

                UINotifyModel = ResourceModel.Get<IUINotifyModel>();
                UINotifyModel.Attach(GetType().FullName, (name) => {
                    OnPropertyChanged(name);
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
        public virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
