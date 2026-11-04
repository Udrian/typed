using System.IO;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    // Watches the source files below a folder and reports every changed file that can affect a build
    internal sealed class ProjectFileWatcher : IDisposable
    {
        private static readonly string[] SourceExtensions = { ".cs", ".csproj", ".sln", ".slnx", ".props", ".targets", ".axaml", ".xaml", ".resx" };
        private static readonly string[] IgnoredFolders = { "bin", "obj", ".git", ".vs", "typer", "node_modules" };

        public string Root { get; }

        private readonly Action<string> SourceChanged;
        private readonly ILogModel LogModel;
        private readonly FileSystemWatcher Watcher;

        public ProjectFileWatcher(string root, Action<string> sourceChanged, ILogModel logModel)
        {
            Root = root;
            SourceChanged = sourceChanged;
            LogModel = logModel;
            Watcher = new FileSystemWatcher(root)
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

        public static bool IsSourceFile(string root, string path)
        {
            var extension = Path.GetExtension(path);
            if (!SourceExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (Path.GetFileName(path).StartsWith("~", StringComparison.Ordinal))
                return false;

            var pathSegments = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !pathSegments.Any(segment => IgnoredFolders.Any(ignored => ignored.Equals(segment, StringComparison.OrdinalIgnoreCase)));
        }

        public void Dispose()
        {
            Watcher.EnableRaisingEvents = false;
            Watcher.Dispose();
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
            if (IsSourceFile(Root, path))
                SourceChanged(path);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            LogModel.Log($"Source file watcher for '{Root}' failed: {e.GetException()}");
        }
    }
}
