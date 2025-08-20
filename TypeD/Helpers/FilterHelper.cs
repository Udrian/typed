using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TypeD.Helpers
{
    /// <summary>
    /// Provides functionality for filtering text based on specified include, exclude, and general filter criteria.
    /// </summary>
    /// <remarks>This class allows users to define filtering rules using the <see cref="Filters"/>, <see cref="Include"/>,
    /// and <see cref="Exclude"/> properties. The filtering logic is applied through the <see cref="Filter(string)"/>
    /// method, which determines whether a given text matches the defined criteria. Filters are
    /// separated by the <see cref="FilterSeperator"/> character, which defaults to a semicolon (";").</remarks>
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
        /// <summary>
        /// Gets or sets the filter criteria used to refine the results of a query or operation.
        /// </summary>
        /// <remarks>The value of this property is used to specify conditions for filtering data.</remarks>
        public string Filters { get; set; }
        /// <summary>
        /// Gets or sets a string that specifies patterns or criteria to exclude certain items.
        /// </summary>
        public string Exclude { get; set; }
        /// <summary>
        /// Gets or sets the items to include in the operation, specified as a delimited string set in <see cref="FilterSeperator"/>.
        /// </summary>
        public string Include { get; set; }
        /// <summary>
        /// Gets or sets the character used to separate multiple filters in a filter string. Defaults to a semicolon (";")
        /// </summary>
        public string FilterSeperator { get; set; } = ";";

        // Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterHelper"/> class with default values.
        /// </summary>
        /// <remarks>The default values for the properties are as follows: <list type="bullet">
        /// <item><description><see cref="Filters"/> is initialized to an empty string.</description></item>
        /// <item><description><see cref="Exclude"/> is initialized to an empty string.</description></item>
        /// <item><description><see cref="Include"/> is initialized to an empty string.</description></item>
        /// </list></remarks>
        public FilterHelper()
        {
            Filters = "";
            Exclude = "";
            Include = "";
        }

        // Functions
        /// <summary>
        /// Determines whether the specified text should be excluded based on the defined filters, include, and exclude lists.
        /// </summary>
        /// <remarks>The method evaluates the text against three criteria: <list type="bullet"> <item>
        /// <description>If the text does not match any of the filters, it is excluded.</description> </item> <item>
        /// <description>If the text is present in the exclude list, it is excluded.</description> </item> <item>
        /// <description>If the text is present in the include list, it is explicitly included, overriding other
        /// criteria.</description> </item> </list> The filters, include, and exclude lists are defined as delimited
        /// strings and are split using the <see cref="FilterSeperator"/>.</remarks>
        /// <param name="text">The text to evaluate against the filters.</param>
        /// <returns>true if the text should be excluded based on the filters and exclude list; otherwise, <see langword="false"/>
        /// if the text is explicitly included or does not match exclusion criteria.</returns>
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
