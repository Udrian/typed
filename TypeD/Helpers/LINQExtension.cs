namespace TypeD.Helpers
{
    public static class LINQExtension
    {
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
