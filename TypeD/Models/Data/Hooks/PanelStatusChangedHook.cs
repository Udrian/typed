using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired when a panel is opened, closed, attached or detached.
    /// </summary>
    public class PanelStatusChangedHook : Hook
    {
        /// <summary>The state change a panel went through.</summary>
        public enum PanelStatus
        {
            /// <summary>The panel was opened.</summary>
            Opened,
            /// <summary>The panel was closed.</summary>
            Closed,
            /// <summary>The panel was attached to the main window.</summary>
            Attached,
            /// <summary>The panel was detached into its own window.</summary>
            Detached
        }

        /// <summary>Gets or sets the panel whose status changed.</summary>
        public Panel Panel { get; set; }
        /// <summary>Gets or sets the new status of the panel.</summary>
        public PanelStatus Status { get; set; }
    }
}
