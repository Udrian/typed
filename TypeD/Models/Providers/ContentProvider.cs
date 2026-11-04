using Avalonia.Media.Imaging;
using TypeD.Models.Data;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ContentProvider : IContentProvider
    {
        private Dictionary<string, Type> SupportedContentTypes { get; set; }
        private Dictionary<string, ContentTypeAssociation> Associations { get; } = new();
        private Dictionary<string, string> Icons { get; } = new(StringComparer.OrdinalIgnoreCase);
        private readonly object SyncRoot = new();

        public event Action ContentTypesChanged;

        public ContentProvider()
        {
            SupportedContentTypes = new Dictionary<string, Type>();
        }

        public List<string> ListAllPaths(Project project)
        {
            string pattern;
            lock (SyncRoot)
                pattern = string.Concat(SupportedContentTypes.Keys.Select(s => "*." + s));
            return Directory.EnumerateFiles(project.ProjectContentPath, pattern, SearchOption.AllDirectories).ToList();
        }
        public async Task<Content> GetContentAsync(string path)
        {
            Type contentType;
            var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            lock (SyncRoot)
                SupportedContentTypes.TryGetValue(extension, out contentType);

            return await Task<Content>.Run(() =>
            {
                var fileBytes = File.ReadAllBytes(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (contentType != null)
                {
                    var content = Activator.CreateInstance(contentType) as Content;
                    content.Path = path;
                    content.Name = fileName;
                    content.Data = fileBytes;
                    content.Initialize();
                    return content;
                }

                return new Content { Path = path, Name = fileName, Data = fileBytes};
            });
        }

        public void AddSupportedContentType<C>(string extension, string icon = null) where C : Content
        {
            lock (SyncRoot)
                RegisterContentType<C>(extension, icon);
            ContentTypesChanged?.Invoke();
        }

        public void AddSupportedContentType<C, TBase, TImplementation>(string extension, string icon = null, string defaultComponent = null, string defaultProperty = null) where C : Content where TImplementation : TBase
        {
            lock (SyncRoot)
            {
                RegisterContentType<C>(extension, icon);
                Associations[extension] = new ContentTypeAssociation(extension, typeof(C), typeof(TBase), typeof(TImplementation), defaultComponent, defaultProperty);
            }
            ContentTypesChanged?.Invoke();
        }

        private void RegisterContentType<C>(string extension, string icon) where C : Content
        {
            SupportedContentTypes.Add(extension, typeof(C));
            if (icon != null)
                Icons[extension] = icon;
        }

        public void RemoveSupportedContentType<C>(string extension) where C : Content
        {
            bool removed;
            lock (SyncRoot)
            {
                removed = SupportedContentTypes.Remove(extension);
                Associations.Remove(extension);
                Icons.Remove(extension);
            }
            if (removed)
                ContentTypesChanged?.Invoke();
        }

        public string GetIcon(string extension)
        {
            extension = extension?.TrimStart('.').ToLowerInvariant();
            lock (SyncRoot)
                return extension != null && Icons.TryGetValue(extension, out var icon) ? icon : null;
        }

        public IReadOnlyList<ContentTypeAssociation> ListAssociations()
        {
            lock (SyncRoot)
                return Associations.Values.ToList();
        }

        public ContentTypeAssociation GetAssociation(string extension)
        {
            lock (SyncRoot)
                return Associations.TryGetValue(extension, out var association) ? association : null;
        }

        public IReadOnlyList<ContentTypeAssociation> GetAssociationsForEngineType(Type engineBaseType)
        {
            lock (SyncRoot)
                return Associations.Values.Where(a => engineBaseType.IsAssignableFrom(a.EngineBaseType)).ToList();
        }
    }
}
