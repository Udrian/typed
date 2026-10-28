using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    public class PanelStatusChangedHook : Hook
    {
        public enum PanelStatus
        {
            Opened,
            Closed,
            Attached,
            Detached
        }

        public Panel Panel { get; set; }
        public PanelStatus Status { get; set; }
    }
}
