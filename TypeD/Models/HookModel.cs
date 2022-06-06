using System;
using System.Collections.Generic;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class HookModel : IHookModel
    {
        private class HookAction
        {
            public Action<object> Action { get; set; }
            public object Raw { get; set; }

            public HookAction(Action<object> action, object raw)
            {
                Action = action;
                Raw = raw;
            }
        }

        private Dictionary<string, List<HookAction>> Hooks { get; set; }

        // Constructors
        public HookModel()
        {
            ClearHooks();
        }

        public void Init(IResourceModel resourceModel)
        {
        }

        // Functions
        public void ClearHooks()
        {
            Hooks = new Dictionary<string, List<HookAction>>();
        }

        public void AddHook(string hook, Action<object> action)
        {
            AddHook(hook, action, action);
        }

        public void AddHook<T>(Action<T> action) where T : Hook, new()
        {
            AddHook(GetName<T>(), (a) => { action(a as T); }, action);
        }

        private void AddHook(string hook, Action<object> action, object raw)
        {
            if (!Hooks.ContainsKey(hook))
                Hooks.Add(hook, new List<HookAction>());
            Hooks[hook].Add(new HookAction(action, raw));
        }

        public void RemoveHook(string hook)
        {
            if(Hooks.ContainsKey(hook))
                Hooks.Remove(hook);
        }

        public void RemoveHook(string hook, Action<object> action)
        {
            if (!Hooks.ContainsKey(hook)) return;
            if(Hooks[hook].Count == 0)
            {
                RemoveHook(hook);
                return;
            }
            RemoveHook(hook, action as object);
        }

        public void RemoveHook<T>() where T : Hook, new()
        {
            RemoveHook(GetName<T>());
        }

        public void RemoveHook<T>(Action<T> action) where T : Hook, new()
        {
            RemoveHook(GetName<T>(), action);
        }

        public void RemoveHook(string hook, object raw)
        {
            Hooks[hook].RemoveAll(h => h.Raw == raw);
        }

        public void Shoot(string hook, object param)
        {
            if (!Hooks.ContainsKey(hook)) return;
            foreach(var h in Hooks[hook])
            {
                h.Action(param);
            }
        }

        public void Shoot<T>(T hook) where T : Hook, new()
        {
            Shoot(GetName<T>(), hook);
        }

        private string GetName<T>()
        {
            var type = typeof(T);
            var name = type.FullName;
            return name.EndsWith("Hook") ? name.Substring(0, name.Length - "Hook".Length) : name;
        }
    }
}
