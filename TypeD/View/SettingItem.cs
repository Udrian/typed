using TypeD.Models.Data;

namespace TypeD.View
{
    /// <summary>A setting shown in the settings view.</summary>
    public class SettingItem
    {
        // Properties
        /// <summary>Gets or sets the title.</summary>
        public string Title { get; set; }
        /// <summary>Gets or sets the description.</summary>
        public string Description { get; set; }
        /// <summary>Gets or sets the category the setting is grouped under.</summary>
        public string Category { get; set; }
        /// <summary>Gets or sets the setting that provides the value.</summary>
        public ISetting Setting { get; set; }
        /// <summary>Gets or sets the action that applies a new value.</summary>
        public Action<object> Set { get; set; }

        // Constructors
        /// <summary>Initializes a new instance of the <see cref="SettingItem"/> class.</summary>
        /// <param name="title">The title.</param>
        /// <param name="description">The description.</param>
        /// <param name="setting">The setting that provides the value.</param>
        /// <param name="set">The action that applies a new value.</param>
        /// <param name="category">The category the setting is grouped under.</param>
        public SettingItem(string title, string description, ISetting setting, Action<object> set, string category = "General")
        {
            Title = title;
            Description = description;
            Category = category;
            Setting = setting;
            Set = set;
        }
    }
}
