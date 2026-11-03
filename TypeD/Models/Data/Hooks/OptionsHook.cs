using System.Collections.Generic;
using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when the options dialog collects settings, so listeners can add setting items.
    /// </summary>
    public class OptionsHook : Hook
    {
        /// <summary>Gets or sets the level (e.g. application or project) the options are collected for.</summary>
        public SettingLevel Level { get; set; }
        /// <summary>Gets or sets the setting items that listeners add to.</summary>
        public List<SettingItem> Items { get; set; }

        /// <summary>Initializes a new instance of the <see cref="OptionsHook"/> class with an empty item list.</summary>
        public OptionsHook()
        {
            Items = new List<SettingItem>();
        }
    }
}
