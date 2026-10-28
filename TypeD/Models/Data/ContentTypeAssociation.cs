using System;

namespace TypeD.Models.Data
{
    /// <summary>
    /// Links the editor representation of a content file to the engine content types that load it.
    /// </summary>
    /// <param name="Extension">File extension, without the dot.</param>
    /// <param name="EditorType">The <see cref="Content"/> type used by the editor.</param>
    /// <param name="EngineBaseType">The engine's abstract content type, e.g. Texture. Used by properties to ask for content of that kind.</param>
    /// <param name="EngineImplementationType">The engine type implementing <see cref="EngineBaseType"/> that loads the file, e.g. TKTexture.</param>
    public record ContentTypeAssociation(string Extension, Type EditorType, Type EngineBaseType, Type EngineImplementationType);
}
