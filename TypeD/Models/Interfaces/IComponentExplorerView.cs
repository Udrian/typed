namespace TypeD.Models.Interfaces
{
    /// <summary>Allows editor commands to focus the component explorer without a concrete view reference.</summary>
    public interface IComponentExplorerView
    {
        /// <summary>Moves keyboard focus to the component tree.</summary>
        void FocusTree();
    }
}