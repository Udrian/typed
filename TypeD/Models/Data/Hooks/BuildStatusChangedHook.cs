namespace TypeD.Models.Data.Hooks
{
    public class BuildStatusChangedHook : Hook
    {
        public Project Project { get; set; }
        public bool IsBuilding { get; set; }
        public bool IsComplete { get; set; }
        public bool Succeeded { get; set; }
        public string Status { get; set; }
        public List<BuildIssue> Warnings { get; set; } = new();
        public List<BuildIssue> Errors { get; set; } = new();
    }
}
