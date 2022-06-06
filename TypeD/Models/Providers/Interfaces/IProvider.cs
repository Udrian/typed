using TypeD.Models.Interfaces;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IProvider
    {
        void Init(IResourceModel resourceModel);
    }
}
