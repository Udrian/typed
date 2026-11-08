using TypeD.Helpers;
using TypeD.Models.Data;

namespace TypeD.Models
{
    /// <summary>Validates component-tree moves independently of the command that executes them.</summary>
    public static class ComponentMoveRules
    {
        /// <summary>Resolves a move destination and insertion index, or returns false for an invalid or unchanged move.</summary>
        public static bool TryResolve(Component node, Component target, MovePosition position, out Component newParent, out int index)
        {
            newParent = null;
            index = -1;
            if (node == null || target == null || node.Removed || node.ParentComponent == null || node.ParentComponent.ReadOnly)
                return false;
            for (var ancestor = target; ancestor != null; ancestor = ancestor.ParentComponent)
            {
                if (ReferenceEquals(ancestor, node))
                    return false;
            }
            newParent = position is MovePosition.Child or MovePosition.FirstChild ? target : target.ParentComponent;
            if (newParent == null || newParent.ReadOnly)
                return false;
            for (var ancestor = newParent; ancestor != null; ancestor = ancestor.ParentComponent)
            {
                if (ancestor.Removed)
                    return false;
            }
            var filter = new FilterHelper();
            newParent.Template?.ChildrenFilter(filter);
            if (!(filter.Filters ?? string.Empty).Contains($"{node.RootBaseComponent.FullName};"))
                return false;
            var siblings = newParent.Children.Where(child => !ReferenceEquals(child, node)).ToList();
            switch (position)
            {
                case MovePosition.Child:
                    index = siblings.Count;
                    break;
                case MovePosition.FirstChild:
                    index = 0;
                    break;
                case MovePosition.Before:
                    index = siblings.IndexOf(target);
                    break;
                default:
                    index = siblings.IndexOf(target) + 1;
                    break;
            }
            return index >= 0 && !(ReferenceEquals(node.ParentComponent, newParent) && node.ParentComponent.Children.IndexOf(node) == index);
        }
    }
}