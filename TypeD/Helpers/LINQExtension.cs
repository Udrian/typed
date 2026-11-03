namespace TypeD.Helpers
{
    /// <summary>
    /// LINQ extension methods.
    /// </summary>
    public static class LINQExtension
    {
        /// <summary>Flattens a hierarchy into a list of all items, each followed by its descendants.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="e">The root items.</param>
        /// <param name="f">Gets the children of an item.</param>
        /// <returns>All items in depth-first order.</returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> e, Func<T, IEnumerable<T>> f)
        {
            List<T> list = new List<T>();
            foreach (var item in e)
            {
                list.Add(item);
                list.AddRange(f(item).Flatten(f));
            }
            return list;
        }
    }
}
