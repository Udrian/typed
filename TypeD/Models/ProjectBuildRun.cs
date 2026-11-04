using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    // The state of one build of a project and its changed modules, published as BuildStatusChangedHooks while it runs
    internal sealed class ProjectBuildRun
    {
        private readonly IHookModel HookModel;
        private readonly Project Project;
        private readonly int StepCount;
        private readonly object SyncRoot = new();
        private readonly List<BuildIssue> Warnings = new();
        private readonly List<BuildIssue> Errors = new();
        private readonly HashSet<string> IssueKeys = new();
        private int StepIndex = -1;
        private string Status = "Building project...";
        private string SubTask;
        private int? SubProgress;
        private int? ExpectedProjects;
        private int BuiltProjects;

        public ProjectBuildRun(IHookModel hookModel, Project project, int stepCount)
        {
            HookModel = hookModel;
            Project = project;
            StepCount = Math.Max(stepCount, 1);
        }

        public int ErrorCount
        {
            get { lock (SyncRoot) return Errors.Count; }
        }

        public void BeginStep(string status, string subTask = null, int? expectedProjects = null)
        {
            lock (SyncRoot)
            {
                StepIndex = Math.Min(StepIndex + 1, StepCount - 1);
                Status = status;
                SubTask = subTask;
                ExpectedProjects = expectedProjects > 0 ? expectedProjects : null;
                BuiltProjects = 0;
                SubProgress = ExpectedProjects.HasValue ? 0 : null;
            }
            Publish();
        }

        public void UpdateSubTask(string subTask, int? subProgress = null)
        {
            lock (SyncRoot)
            {
                SubTask = subTask;
                SubProgress = subProgress.HasValue ? Math.Clamp(subProgress.Value, 0, 100) : null;
            }
            Publish();
        }

        // Called when the build output reports that a C# project finished building
        public void ProjectBuilt(string projectName)
        {
            lock (SyncRoot)
            {
                BuiltProjects++;
                SubTask = $"Built {projectName}";
                if (ExpectedProjects.HasValue)
                    SubProgress = Math.Min(100, 100 * BuiltProjects / ExpectedProjects.Value);
            }
            Publish();
        }

        public void AddIssue(BuildIssue issue)
        {
            lock (SyncRoot)
            {
                // The build output repeats every warning and error in its summary, and a module can be built more than once
                if (!IssueKeys.Add($"{issue.Source}|{issue}"))
                    return;

                (issue.Severity == "warning" ? Warnings : Errors).Add(issue);
            }
        }

        public void Complete(bool succeeded, string status)
        {
            Publish(isBuilding: false, isComplete: true, succeeded, status);
        }

        private void Publish()
        {
            Publish(isBuilding: true, isComplete: false, succeeded: false, status: null);
        }

        private void Publish(bool isBuilding, bool isComplete, bool succeeded, string status)
        {
            BuildStatusChangedHook hook;
            lock (SyncRoot)
            {
                var stepProgress = SubProgress ?? 0;
                hook = new BuildStatusChangedHook
                {
                    Project = Project,
                    IsBuilding = isBuilding,
                    IsComplete = isComplete,
                    Succeeded = succeeded,
                    Status = status ?? Status,
                    Progress = isComplete ? 100 : Math.Clamp((Math.Max(StepIndex, 0) * 100 + stepProgress) / StepCount, 0, 100),
                    SubTask = isComplete ? null : SubTask,
                    SubProgress = isComplete ? null : SubProgress,
                    Warnings = new List<BuildIssue>(Warnings),
                    Errors = new List<BuildIssue>(Errors)
                };
            }
            HookModel.Shoot(hook);
        }
    }
}
