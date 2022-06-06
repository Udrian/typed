using System.Collections.Generic;
using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    public class OptionsHook : Hook
    {
        public SettingLevel Level { get; set; }
        public List<SettingItem> Items { get; set; }

        public OptionsHook()
        {
            Items = new List<SettingItem>();
        }
    }
}
