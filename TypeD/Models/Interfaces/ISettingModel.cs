using System;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that manages setting contexts.</summary>
    public interface ISettingModel : IModel
    {
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
