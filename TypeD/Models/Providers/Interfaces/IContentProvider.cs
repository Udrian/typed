using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IContentProvider
    {
        List<string> ListAllPaths(Project project);

        Task<Content> GetContentAsync(string path);

        void AddSupportedContentType<C>(string extension) where C : Content;

        /// <summary>
        /// Adds a supported content type and associates it with the engine types that load it.
        /// </summary>
        /// <typeparam name="C">The editor content type.</typeparam>
        /// <typeparam name="TBase">The engine base content type, e.g. Texture.</typeparam>
        /// <typeparam name="TImplementation">The engine implementation of <typeparamref name="TBase"/>, e.g. TKTexture.</typeparam>
        void AddSupportedContentType<C, TBase, TImplementation>(string extension) where C : Content where TImplementation : TBase;

        void RemoveSupportedContentType<C>(string extension) where C : Content;

        /// <summary>
        /// Lists all associations between editor content, engine base content and engine implementations.
        /// </summary>
        IReadOnlyList<ContentTypeAssociation> ListAssociations();

        /// <summary>
        /// Gets the association for a file extension, or null if it has none.
        /// </summary>
        ContentTypeAssociation GetAssociation(string extension);

        /// <summary>
        /// Lists the associations whose engine base type is, or derives from, <paramref name="engineBaseType"/>.
        /// </summary>
        IReadOnlyList<ContentTypeAssociation> GetAssociationsForEngineType(Type engineBaseType);
    }
}
