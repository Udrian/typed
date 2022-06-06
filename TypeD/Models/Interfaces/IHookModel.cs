using System;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface IHookModel : IModel
    {
        public void ClearHooks();
        public void AddHook(string hook, Action<object> action);
        public void AddHook<T>(Action<T> action) where T : Hook, new();
        public void RemoveHook(string hook);
        public void RemoveHook(string hook, Action<object> action);
        public void RemoveHook<T>() where T : Hook, new();
        public void RemoveHook<T>(Action<T> action) where T : Hook, new();
        public void Shoot(string hook, object param);
        public void Shoot<T>(T hook) where T : Hook, new();
    }
}
