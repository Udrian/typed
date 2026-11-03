using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace TypeD.Models.Data
{
    /// <summary>The level a setting is stored at; later levels override earlier ones.</summary>
    public enum SettingLevel
    {
        /// <summary>Settings shared by all workspaces of the user.</summary>
        System = 0,
        /// <summary>Settings of the current workspace.</summary>
        Workspace,
        /// <summary>Settings local to the current project.</summary>
        Local
    }

    /// <summary>Gives a setting context the name used for its setting file.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NameAttribute : Attribute
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Initializes a new instance of the <see cref="NameAttribute"/> class.</summary>
        /// <param name="name">The name.</param>
        public NameAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>Untyped access to a setting value.</summary>
    public interface ISetting
    {
        /// <summary>Sets the value.</summary>
        /// <param name="value">The new value, which must match the setting's value type.</param>
        public void SetValue(object value);
        /// <summary>Gets the value.</summary>
        /// <returns>The current value.</returns>
        public object GetValue();
        /// <summary>Gets the default value.</summary>
        /// <returns>The default value.</returns>
        public object GetDefaultValue();
        /// <summary>Sets the level the value comes from.</summary>
        /// <param name="level">The level.</param>
        public void SetLevel(SettingLevel level);
        /// <summary>Gets the level the value comes from.</summary>
        /// <returns>The level.</returns>
        public SettingLevel GetLevel();
    }

    /// <summary>A single typed setting with a default value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public class Setting<T> : ISetting
    {
        // Properties
        /// <summary>Gets the level the value comes from.</summary>
        [JsonIgnore]
        public SettingLevel Level { get; internal set; }

        /// <summary>Gets the default value.</summary>
        [JsonIgnore]
        public T DefaultValue { get; internal set; }
        /// <summary>Gets or sets the current value.</summary>
        public T Value { get; set; }

        // Constructors
        /// <summary>Initializes a new instance of the <see cref="Setting{T}"/> class with the value set to the default.</summary>
        /// <param name="defaultValue">The default value.</param>
        public Setting(T defaultValue)
        {
            Value = DefaultValue = defaultValue;
        }

        // Function
        /// <inheritdoc/>
        public void SetValue(object value)
        {
            Value = (T)value;
        }

        /// <inheritdoc/>
        public object GetValue()
        {
            return Value;
        }

        /// <inheritdoc/>
        public object GetDefaultValue()
        {
            return DefaultValue;
        }

        /// <inheritdoc/>
        public void SetLevel(SettingLevel level)
        {
            Level = level;
        }

        /// <inheritdoc/>
        public SettingLevel GetLevel()
        {
            return Level;
        }
    }

    /// <summary>
    /// Base class for a group of related settings that is stored together in one setting file.
    /// </summary>
    public class SettingContext
    {
        // Properties
        /// <summary>Gets the level the context was loaded from.</summary>
        [JsonIgnore]
        public SettingLevel Level { get; internal set; }
        /// <summary>Gets or sets a value indicating whether the context is saved when the application exits.</summary>
        [JsonIgnore]
        public bool SaveOnExit { get; set; }
        /// <summary>Gets a value indicating whether the context has changes that still need to be saved.</summary>
        [JsonIgnore]
        public bool ShouldSave { get; internal set; }

        // Constructors
        internal SettingContext() { }

        internal void Init(SettingLevel settingLevel)
        {
            Level = settingLevel;

            foreach (var property in GetType().GetProperties())
            {
                if (property.PropertyType.GetInterfaces().Contains(typeof(ISetting)))
                {
                    var settingProperty = property.GetValue(this) as ISetting;
                    settingProperty.SetLevel(settingLevel);
                }
            }
        }

        // Functions
        internal SettingContext Merge(SettingContext mergeContext)
        {
            var thisType = GetType();
            var newContext = Activator.CreateInstance(thisType) as SettingContext;
            newContext.Level = Level;

            foreach (var property in thisType.GetProperties())
            {
                if (!property.PropertyType.GetInterfaces().Contains(typeof(ISetting))) continue;
               
                var settingProperty = property.GetValue(this) as ISetting;
                var newSettingProperty = property.GetValue(newContext) as ISetting;
                var mergedSettingProperty = property.GetValue(mergeContext) as ISetting;
                if (!mergedSettingProperty.GetValue().Equals(settingProperty.GetDefaultValue()))
                {
                    newSettingProperty.SetValue(mergedSettingProperty.GetValue());
                    newSettingProperty.SetLevel(mergedSettingProperty.GetLevel());
                }
                else
                {
                    newSettingProperty.SetValue(settingProperty.GetValue());
                    newSettingProperty.SetLevel(settingProperty.GetLevel());
                }
            }

            return newContext;
        }
    }
}
