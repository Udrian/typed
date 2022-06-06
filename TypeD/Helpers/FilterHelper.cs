using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TypeD.Helpers
{
    public class FilterHelper
    {
        private class FilterComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return y.ToLower().Contains(x.ToLower());
            }

            public int GetHashCode([DisallowNull] string obj)
            {
                return obj.GetHashCode();
            }
        }

        // Properties
        public string Filters { get; set; }
        public string Exclude { get; set; }
        public string Include { get; set; }
        public string FilterSeperator { get; set; } = ";";

        // Constructors
        public FilterHelper()
        {
            Filters = "";
            Exclude = "";
            Include = "";
        }

        // Functions
        public bool Filter(string text)
        {
            var filters = Filters.Split(FilterSeperator).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
            var exclude = Exclude.Split(FilterSeperator).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
            var include = Include.Split(FilterSeperator).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));

            var filterResult = false;
            if (filters.Any(f => f != "") && !filters.Contains(text, new FilterComparer())) filterResult = true;
            if (exclude.Contains(text)) filterResult = true;
            if (include.Contains(text)) filterResult = false;

            return filterResult;
        }
    }
}
