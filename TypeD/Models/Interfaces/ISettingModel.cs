using System;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    public interface ISettingModel : IModel
    {
        public void InitContext<T>(Project project) where T : SettingContext, new();
        public void RemoveContext<T>() where T : SettingContext, new();
        public T GetContext<T>(SettingLevel settingLevel = SettingLevel.Local) where T : SettingContext, new();
        public void SetContext(SettingContext settingContext, SettingLevel? settingLevel = null);
        public string GetName<T>() where T : SettingContext, new();
        public string GetName(Type type);
        public string GetPath(Project project, SettingLevel settingLevel);
    }
}
