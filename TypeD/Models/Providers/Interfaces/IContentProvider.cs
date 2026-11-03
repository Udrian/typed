using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    /// <summary>Provider of project content files and their supported types.</summary>
    public interface IContentProvider
    {
        /// <summary>Lists the paths of all content files in a project.</summary>
        /// <param name="project">The project.</param>
        /// <returns>The paths.</returns>
        List<string> ListAllPaths(Project project);

        /// <summary>Loads the content at a path.</summary>
        /// <param name="path">The content file path.</param>
        /// <returns>The content, or <c>null</c> if the type is unsupported.</returns>
        Task<Content> GetContentAsync(string path);

        /// <summary>Adds a supported content type for a file extension.</summary>
        /// <typeparam name="C">The editor content type.</typeparam>
        /// <param name="extension">The file extension.</param>
        /// <param name="icon">File name of the icon shown on content of this type.</param>
        void AddSupportedContentType<C>(string extension, string icon = null) where C : Content;

        /// <summary>
        /// Adds a supported content type and associates it with the engine types that load it.
        /// </summary>
        /// <typeparam name="C">The editor content type.</typeparam>
        /// <typeparam name="TBase">The engine base content type, e.g. Texture.</typeparam>
        /// <typeparam name="TImplementation">The engine implementation of <typeparamref name="TBase"/>, e.g. TKTexture.</typeparam>
        /// <param name="extension">The file extension.</param>
        /// <param name="icon">File name of the icon shown on content of this type. It must exist in an icon folder registered with <see cref="TypeD.Models.Interfaces.IIconModel.AddDefaultIconsPath"/>.</param>
        /// <param name="defaultComponent">Full name of the component created when content of this type is dropped in the viewer.</param>
        /// <param name="defaultProperty">Name of the property on the default component that receives the content.</param>
        void AddSupportedContentType<C, TBase, TImplementation>(string extension, string icon = null, string defaultComponent = null, string defaultProperty = null) where C : Content where TImplementation : TBase;

        /// <summary>Removes the supported content type for a file extension.</summary>
        /// <typeparam name="C">The editor content type.</typeparam>
        /// <param name="extension">The file extension.</param>
        void RemoveSupportedContentType<C>(string extension) where C : Content;

        /// <summary>
        /// Gets the icon file name declared for a file extension, or null if there is none.
        /// </summary>
        string GetIcon(string extension);

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
