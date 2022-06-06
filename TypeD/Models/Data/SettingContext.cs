using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace TypeD.Models.Data
{
    public enum SettingLevel
    {
        System = 0,
        Workspace,
        Local
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NameAttribute : Attribute
    {
        public string Name { get; set; }
        public NameAttribute(string name)
        {
            Name = name;
        }
    }

    public interface ISetting
    {
        public void SetValue(object value);
        public object GetValue();
        public object GetDefaultValue();
        public void SetLevel(SettingLevel level);
        public SettingLevel GetLevel();
    }

    public class Setting<T> : ISetting
    {
        // Properties
        [JsonIgnore]
        public SettingLevel Level { get; internal set; }

        [JsonIgnore]
        public T DefaultValue { get; internal set; }
        public T Value { get; set; }

        // Constructors
        public Setting(T defaultValue)
        {
            Value = DefaultValue = defaultValue;
        }

        // Function
        public void SetValue(object value)
        {
            Value = (T)value;
        }

        public object GetValue()
        {
            return Value;
        }

        public object GetDefaultValue()
        {
            return DefaultValue;
        }

        public void SetLevel(SettingLevel level)
        {
            Level = level;
        }

        public SettingLevel GetLevel()
        {
            return Level;
        }
    }

    public class SettingContext
    {
        // Properties
        [JsonIgnore]
        public SettingLevel Level { get; internal set; }
        [JsonIgnore]
        public bool SaveOnExit { get; set; }
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
