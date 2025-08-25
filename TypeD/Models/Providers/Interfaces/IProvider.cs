using TypeD.Models.Interfaces;

namespace TypeD.Models.Providers.Interfaces
{
    /// <summary>
    /// Defines a contract for initializing a provider with a specified resource model.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for configuring or preparing the provider
    /// using the provided resource model. The behavior of the provider after initialization depends on the specific
    /// implementation.</remarks>
    public interface IProvider
    {
        /// <summary>
        /// Initializes the system with the specified resource model.
        /// </summary>
        /// <param name="resourceModel">The resource model to be used for initialization. Cannot be null.</param>
        void Init(IResourceModel resourceModel);
    }
}
