namespace TypeD.Models.Data.Hooks
{
    /// <summary>Captures command parameters before the command palette takes keyboard focus.</summary>
    public class CommandPaletteContextHook : Hook
    {
        /// <summary>Gets parameter overrides keyed by editor command id.</summary>
        public Dictionary<string, object> Parameters { get; } = new();
    }
}
