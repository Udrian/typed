namespace TypeD.Models.Data.Hooks
{
    public class ComponentFocusHook : Hook
    {
        public Project Project { get; set; }
        public Component Component { get; set; }
        public bool Root { get; set; } = true;
        /// <summary>
        /// The full multi-selection, with <see cref="Component"/> being the focused one. Null means only <see cref="Component"/> is selected.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Component> Selection { get; set; }
    }
}
