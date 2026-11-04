using System.IO;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    // Watches the source of a project and of its local modules, and requests one build for all changes made close together
    internal sealed class ProjectWatch : IDisposable
    {
        private const int DebounceMilliseconds = 750;

        private readonly Project Project;
        private readonly Action<Project, IReadOnlyList<Module>> BuildRequested;
        private readonly ILogModel LogModel;
        private readonly Timer DebounceTimer;
        private readonly object SyncRoot = new();
        private readonly Dictionary<string, ProjectFileWatcher> Watchers = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Module> ChangedModules = new();
        private bool GameChanged;
        private bool IsDisposed;

        public ProjectWatch(Project project, Action<Project, IReadOnlyList<Module>> buildRequested, ILogModel logModel)
        {
            Project = project;
            BuildRequested = buildRequested;
            LogModel = logModel;
            DebounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
            Refresh();
        }

        public IReadOnlyCollection<string> Roots
        {
            get { lock (SyncRoot) return Watchers.Keys.ToList(); }
        }

        // Matches the watched folders with the project's current local modules
        public void Refresh()
        {
            var roots = new List<string> { NormalizePath(Project.Location) };
            foreach (var module in LocalModules())
            {
                var root = NormalizePath(module.ModulePath);
                // A module inside another watched folder is already watched through that folder
                if (Directory.Exists(root) && !roots.Any(r => IsWithin(root, r) || IsWithin(r, root)))
                    roots.Add(root);
            }

            lock (SyncRoot)
            {
                if (IsDisposed)
                    return;

                foreach (var removed in Watchers.Keys.Where(root => !roots.Contains(root, StringComparer.OrdinalIgnoreCase)).ToList())
                {
                    Watchers[removed].Dispose();
                    Watchers.Remove(removed);
                }

                foreach (var root in roots.Where(root => !Watchers.ContainsKey(root) && Directory.Exists(root)))
                {
                    try
                    {
                        Watchers.Add(root, new ProjectFileWatcher(root, SourceChanged, LogModel));
                    }
                    catch (Exception e)
                    {
                        LogModel.Log($"Could not watch '{root}' for source changes: {e.Message}");
                    }
                }
            }
        }

        // Finds the local module a changed file belongs to, or null if it belongs to the game project
        public Module FindModule(string path)
        {
            var fullPath = NormalizePath(path);
            return LocalModules()
                .Where(module => IsWithin(fullPath, NormalizePath(module.ModulePath)))
                .OrderByDescending(module => module.ModulePath.Length)
                .FirstOrDefault();
        }

        public void SourceChanged(string path)
        {
            var module = FindModule(path);
            lock (SyncRoot)
            {
                if (IsDisposed)
                    return;

                if (module != null)
                    ChangedModules.Add(module);
                else
                    GameChanged = true;
                DebounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;

                foreach (var watcher in Watchers.Values)
                    watcher.Dispose();
                Watchers.Clear();
            }

            DebounceTimer.Dispose();
        }

        private IEnumerable<Module> LocalModules()
        {
            return Project.Modules.ToList().Where(module => module.IsLocal && !string.IsNullOrEmpty(module.ModulePath));
        }

        private void OnDebounceElapsed(object state)
        {
            List<Module> modules;
            lock (SyncRoot)
            {
                if (IsDisposed || (!GameChanged && ChangedModules.Count == 0))
                    return;

                // Keep the project's module order so modules are built in the order they were resolved in
                modules = Project.Modules.Where(ChangedModules.Contains).ToList();
                ChangedModules.Clear();
                GameChanged = false;
            }

            BuildRequested(Project, modules);
        }

        private static string NormalizePath(string path)
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }

        private static bool IsWithin(string path, string folder)
        {
            return path.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
