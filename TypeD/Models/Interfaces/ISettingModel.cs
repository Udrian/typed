using System;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that manages setting contexts.</summary>
    public interface ISettingModel : IModel
    {
        /// <summary>Returns whether this setting (or dictionary entry) has an explicit project default.</summary>
        bool HasProjectDefault(ISetting setting, string entry = null);
        /// <summary>Returns whether this setting or dictionary entry is explicitly overridden by the user.</summary>
        bool HasUserOverride(ISetting setting, string entry = null);
        /// <summary>Gets the project default, or the declared fallback when there is none.</summary>
        object GetRevertValue(ISetting setting, string entry = null, object fallback = null);
        /// <summary>Stores or removes just this setting's project default and persists the project context.</summary>
        Task SetProjectDefaultAsync(Project project, ISetting setting, object value, bool remove = false, string entry = null);
        /// <summary>Removes a user override so the setting inherits lower scopes again.</summary>
        void ClearUserOverride(ISetting setting, string entry = null);
        /// <summary>Loads a setting context for a project.</summary>
        /// <typeparam name="T">The setting context type.</typeparam>
        /// <param name="project">The project.</param>
        public void InitContext<T>(Project project) where T : SettingContext, new();
        /// <summary>Removes a setting context.</summary>
        /// <typeparam name="T">The setting context type.</typeparam>
        public void RemoveContext<T>() where T : SettingContext, new();
        /// <summary>Gets a setting context.</summary>
        /// <typeparam name="T">The setting context type.</typeparam>
        /// <param name="settingLevel">The level to get it from.</param>
        /// <returns>The setting context.</returns>
        public T GetContext<T>(SettingLevel settingLevel = SettingLevel.User) where T : SettingContext, new();
        /// <summary>Sets a setting context.</summary>
        /// <param name="settingContext">The context to set.</param>
        /// <param name="settingLevel">The level to set it at, or <c>null</c> for the default.</param>
        public void SetContext(SettingContext settingContext, SettingLevel? settingLevel = null);
        /// <summary>Gets the name of a setting context type.</summary>
        /// <typeparam name="T">The setting context type.</typeparam>
        /// <returns>The name.</returns>
        public string GetName<T>() where T : SettingContext, new();
        /// <summary>Gets the name of a setting context type.</summary>
        /// <param name="type">The setting context type.</param>
        /// <returns>The name.</returns>
        public string GetName(Type type);
        /// <summary>Gets the file path of a setting level.</summary>
        /// <param name="project">The project.</param>
        /// <param name="settingLevel">The setting level.</param>
        /// <returns>The path.</returns>
        public string GetPath(Project project, SettingLevel settingLevel);
    }
}
