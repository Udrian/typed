namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Interface for Models in MVVM pattern
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Called once when initializing the model
        /// </summary>
        /// <param name="resourceModel">Resource model to fetch resources and other models, just for dependency injection</param>
        void Init(IResourceModel resourceModel);
    }
}
