using System.IO;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal sealed class ProjectFileWatcher : IDisposable
    {
        private readonly Project Project;
        private readonly Action<Project> BuildRequested;
        private readonly ILogModel LogModel;
        private readonly FileSystemWatcher Watcher;
        private readonly Timer DebounceTimer;
        private readonly object SyncRoot = new();
        private bool IsDisposed;

        public ProjectFileWatcher(Project project, Action<Project> buildRequested, ILogModel logModel)
        {
            Project = project;
            BuildRequested = buildRequested;
            LogModel = logModel;
            DebounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
            Watcher = new FileSystemWatcher(project.Location)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                InternalBufferSize = 32 * 1024
            };

            Watcher.Changed += OnFileChanged;
            Watcher.Created += OnFileChanged;
            Watcher.Deleted += OnFileChanged;
            Watcher.Renamed += OnFileRenamed;
            Watcher.Error += OnError;
            Watcher.EnableRaisingEvents = true;
        }

        public void ScheduleBuild()
        {
            lock (SyncRoot)
            {
                if (!IsDisposed)
                    DebounceTimer.Change(750, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;
            }

            Watcher.EnableRaisingEvents = false;
            Watcher.Dispose();
            DebounceTimer.Dispose();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            HandlePath(e.FullPath);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            HandlePath(e.FullPath);
            HandlePath(e.OldFullPath);
        }

        private void HandlePath(string path)
        {
            var extension = Path.GetExtension(path);
            if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".props", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".targets", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = Path.GetRelativePath(Project.Location, path);
            var pathSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (pathSegments.Any(segment =>
                segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals(".vs", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (Path.GetFileName(path).StartsWith("~", StringComparison.Ordinal))
                return;

            ScheduleBuild();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            LogModel.Log($"Project source file watcher failed: {e.GetException()}");
        }

        private void OnDebounceElapsed(object state)
        {
            lock (SyncRoot)
            {
                if (IsDisposed)
                    return;
            }

            BuildRequested(Project);
        }
    }
}
