namespace TypeD.Models.Data.Hooks
{
    public class ProjectCreateHook : Hook
    {
        public Project Project { get; private set; }

        public ProjectCreateHook() { }
        public ProjectCreateHook(Project project)
        {
            Project = project;
        }
    }
}
