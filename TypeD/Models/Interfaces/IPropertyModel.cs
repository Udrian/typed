using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Keeps the <see cref="IPropertyDefinition"/> for each property type. Modules register their own definitions
    /// to add custom JSON, code generation and UI handling for a property type.
    /// </summary>
    public interface IPropertyModel : IModel
    {
        /// <summary>
        /// Registers a definition, replacing any existing definition for the same type, and activates its JSON converter.
        /// </summary>
        public void Register(IPropertyDefinition definition);

        /// <summary>
        /// Removes the definition for the type and deactivates its JSON converter.
        /// </summary>
        public void Unregister(Type type);

        /// <summary>
        /// Gets the definition for the type, or <see langword="null"/> if none is registered.
        /// </summary>
        public IPropertyDefinition Get(Type type);

        /// <summary>
        /// Lists all registered definitions.
        /// </summary>
        public IReadOnlyList<IPropertyDefinition> List();
    }
}
