using System;
using System.Runtime.CompilerServices;
using TypeD.ViewModel;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that notifies attached view models about changes.</summary>
    public interface IUINotifyModel : IModel
    {
        /// <summary>Attaches a listener under a key.</summary>
        /// <param name="key">The key, usually a view model type name.</param>
        /// <param name="notifyEvent">Called with a property name when it changed.</param>
        /// <param name="addEvent">Called with an element and whether it was removed.</param>
        public void Attach(string key, Action<string> notifyEvent, Action<object, bool> addEvent = null);
        /// <summary>Attaches a listener for a view model type.</summary>
        /// <typeparam name="T">The view model type.</typeparam>
        /// <param name="notifyEvent">Called with a property name when it changed.</param>
        /// <param name="addEvent">Called with an element and whether it was removed.</param>
        public void Attach<T>(Action<string> notifyEvent, Action<object, bool> addEvent = null) where T : ViewModelBase;
        /// <summary>Detaches the listener under a key.</summary>
        /// <param name="key">The key.</param>
        public void Detach(string key);
        /// <summary>Detaches the listener for a view model type.</summary>
        /// <typeparam name="T">The view model type.</typeparam>
        public void Detach<T>() where T : ViewModelBase;
        /// <summary>Notifies all listeners that a property changed.</summary>
        /// <param name="name">The property name.</param>
        public void Notify([CallerMemberName] string name = null);
        /// <summary>Notifies the listener under a key that a property changed.</summary>
        /// <param name="key">The key.</param>
        /// <param name="name">The property name.</param>
        public void Notify(string key, [CallerMemberName] string name = null);
        /// <summary>Notifies the listener for a view model type that a property changed.</summary>
        /// <typeparam name="T">The view model type.</typeparam>
        /// <param name="name">The property name.</param>
        public void Notify<T>([CallerMemberName] string name = null) where T : ViewModelBase;
        /// <summary>Tells the listener under a key that an element was added.</summary>
        /// <param name="key">The key.</param>
        /// <param name="element">The added element.</param>
        public void AddTo(string key, object element);
        /// <summary>Tells the listener for a view model type that an element was added.</summary>
        /// <typeparam name="T">The view model type.</typeparam>
        /// <param name="element">The added element.</param>
        public void AddTo<T>(object element) where T : ViewModelBase;
        /// <summary>Tells the listener under a key that an element was removed.</summary>
        /// <param name="key">The key.</param>
        /// <param name="element">The removed element.</param>
        public void RemoveFrom(string key, object element);
        /// <summary>Tells the listener for a view model type that an element was removed.</summary>
        /// <typeparam name="T">The view model type.</typeparam>
        /// <param name="element">The removed element.</param>
        public void RemoveFrom<T>(object element) where T : ViewModelBase;
    }
}
