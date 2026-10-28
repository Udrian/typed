using Avalonia.Media.Imaging;
using TypeD.Models.Data;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class ContentProvider : IContentProvider
    {
        private Dictionary<string, Type> SupportedContentTypes { get; set; }
        private Dictionary<string, ContentTypeAssociation> Associations { get; } = new();

        public ContentProvider()
        {
            SupportedContentTypes = new Dictionary<string, Type>();
        }

        public List<string> ListAllPaths(Project project)
        {
            return Directory.EnumerateFiles(project.ProjectContentPath, string.Concat(SupportedContentTypes.Keys.Select(s => "*." + s)), SearchOption.AllDirectories).ToList();
        }
        public async Task<Content> GetContentAsync(string path)
        {
            return await Task<Content>.Run(() =>
            {
                var fileBytes = File.ReadAllBytes(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                var extension = Path.GetExtension(path).TrimStart('.').ToLower();

                if (SupportedContentTypes.TryGetValue(extension, out Type contentType))
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

        public void AddSupportedContentType<C>(string extension) where C : Content
        {
            SupportedContentTypes.Add(extension, typeof(C));
        }

        public void AddSupportedContentType<C, TBase, TImplementation>(string extension) where C : Content where TImplementation : TBase
        {
            AddSupportedContentType<C>(extension);
            Associations[extension] = new ContentTypeAssociation(extension, typeof(C), typeof(TBase), typeof(TImplementation));
        }

        public void RemoveSupportedContentType<C>(string extension) where C : Content
        {
            SupportedContentTypes.Remove(extension);
            Associations.Remove(extension);
        }

        public IReadOnlyList<ContentTypeAssociation> ListAssociations()
        {
            return Associations.Values.ToList();
        }

        public ContentTypeAssociation GetAssociation(string extension)
        {
            return Associations.TryGetValue(extension, out var association) ? association : null;
        }

        public IReadOnlyList<ContentTypeAssociation> GetAssociationsForEngineType(Type engineBaseType)
        {
            return Associations.Values.Where(a => engineBaseType.IsAssignableFrom(a.EngineBaseType)).ToList();
        }
    }
}
