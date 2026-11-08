using Avalonia.Media.Imaging;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Models
{
    /// <summary>
    /// Loads and caches the bitmaps of the component icons stored in the project.
    /// </summary>
    public static class IconCache
    {
        private static readonly Dictionary<string, (DateTime Written, Bitmap Bitmap)> Cache = new();

        /// <summary>Returns the cached bitmap for a project icon.</summary>
        public static Bitmap Get(TypeD.Models.Data.Project project, string iconName)
        {
            var path = ViewModelBase.ResourceModel?.Get<IIconModel>()?.GetPath(project, iconName);
            if (path == null)
                return null;

            try
            {
                var written = File.GetLastWriteTimeUtc(path);
                lock (Cache)
                {
                    if (Cache.TryGetValue(path, out var cached) && cached.Written == written)
                        return cached.Bitmap;

                    using var stream = File.OpenRead(path);
                    var bitmap = new Bitmap(stream);
                    Cache[path] = (written, bitmap);
                    return bitmap;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
