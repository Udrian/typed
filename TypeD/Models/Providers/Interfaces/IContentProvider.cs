using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IContentProvider
    {
        List<string> ListAllPaths(Project project);

        Task<Content> GetContentAsync(string path);

        void AddSupportedContentType<C>(string extension) where C : Content;

        void RemoveSupportedContentType<C>(string extension) where C : Content;
    }
}
