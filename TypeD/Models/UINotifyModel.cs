using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Models
{
    internal class UINotifyModel : IUINotifyModel
    {
        public class UINotify
        {
            public Action<string> PropertyUpdateNotification { get; set; }
            public Action<object, bool> ElementAddNotification { get; set; }
        }

        // Data
        private Dictionary<string, UINotify> Attachments { get; set; }

        // Constructors
        public UINotifyModel()
        {
            Attachments = new Dictionary<string, UINotify>();
        }

        public void Init(IResourceModel resourceModel)
        {
        }

        // Functions
        public void Attach(string key, Action<string> notifyEvent, Action<object, bool> addEvent = null)
        {
            if (Attachments.ContainsKey(key))
                return;
            Attachments.Add(key, new UINotify() {
                PropertyUpdateNotification = notifyEvent,
                ElementAddNotification = addEvent
            });
        }

        public void Attach<T>(Action<string> notifyEvent, Action<object, bool> addEvent = null) where T : ViewModelBase
        {
            Attach(typeof(T).FullName, notifyEvent, addEvent);
        }

        public void Detach(string key)
        {
            if (Attachments.ContainsKey(key))
                Attachments.Remove(key);
        }

        public void Detach<T>() where T : ViewModelBase
        {
            Detach(typeof(T).FullName);
        }

        public void Notify([CallerMemberName] string name = null)
        {
            foreach(var attachment in Attachments)
            {
                attachment.Value.PropertyUpdateNotification.Invoke(name);
            }
        }

        public void Notify(string key, [CallerMemberName] string name = null)
        {
            var ui = GetUINotify(key);
            if (ui != null)
                ui.PropertyUpdateNotification.Invoke(name);
        }

        public void Notify<T>([CallerMemberName] string name = null) where T : ViewModelBase
        {
            Notify(typeof(T).FullName);
        }

        public void AddTo(string key, object element)
        {
            var ui = GetUINotify(key);
            if (ui != null)
                ui.ElementAddNotification.Invoke(element, false);
        }

        public void AddTo<T>(object element) where T : ViewModelBase
        {
            AddTo(typeof(T).FullName, element);
        }

        private UINotify GetUINotify(string key)
        {
            if(Attachments.ContainsKey(key))
                return Attachments[key];
            if(Attachments.ContainsKey($"{key}ViewModel"))
                return Attachments[$"{key}ViewModel"];
            var retKey = Attachments.Keys.FirstOrDefault(k => new List<string>() { key, $"{key}ViewModel" }.Contains(k.Substring(k.LastIndexOf(".")+1)));
            if (retKey != null)
                return Attachments[retKey];

            return null;
        }

        public void RemoveFrom(string key, object element)
        {
            var ui = GetUINotify(key);
            if (ui != null)
                ui.ElementAddNotification.Invoke(element, true);
        }

        public void RemoveFrom<T>(object element) where T : ViewModelBase
        {
            RemoveFrom(typeof(T).FullName, element);
        }
    }
}
