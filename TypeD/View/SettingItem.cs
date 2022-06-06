using System;
using TypeD.Models.Data;

namespace TypeD.View
{
    public class SettingItem
    {
        public enum SettingType
        {
            Text
        }

        // Properties
        public string Title { get; set; }
        public string Description { get; set; }
        public ISetting Setting { get; set; }
        public Action<object> Set { get; set; }

        // Constructors
        public SettingItem(string title, string description, ISetting setting, Action<object> set)
        {
            Title = title;
            Description = description;
            Setting = setting;
            Set = set;
        }
    }
}
