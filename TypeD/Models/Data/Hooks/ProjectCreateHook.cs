namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired after a new project has been created.
    /// </summary>
    public class ProjectCreateHook : Hook
    {
        /// <summary>Gets the project that was created.</summary>
        public Project Project { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="ProjectCreateHook"/> class.</summary>
        public ProjectCreateHook() { }

        /// <summary>Initializes a new instance of the <see cref="ProjectCreateHook"/> class.</summary>
        /// <param name="project">The project that was created.</param>
        public ProjectCreateHook(Project project)
        {
            Project = project;
        }
    }
}
