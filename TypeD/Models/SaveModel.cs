using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class SaveModel : ISaveModel
    {
        // Data
        private Dictionary<Type, SaveContext> SaveContexts { get; set; }
        private readonly SemaphoreSlim SaveGate = new(1, 1);

        // Models
        IResourceModel ResourceModel { get; set; }
        IUINotifyModel UINotifyModel { get; set; }

        // Constructors
        public SaveModel()
        {
            SaveContexts = new Dictionary<Type, SaveContext>();
        }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;

            UINotifyModel = ResourceModel.Get<IUINotifyModel>();
        }

        // Properties
        public bool AnythingToSave { get { return SaveContexts.Any(c => c.Value.ShouldSave); } }
        public event Action<bool> SaveStateChanged;

        private void NotifySaveStateChanged()
        {
            UINotifyModel.Notify("AnythingToSave");
            SaveStateChanged?.Invoke(AnythingToSave);
        }

        public void NotifyAutosaveCompleted(Project project)
        {
            ProjectSaveState.RecordAutosave(project);
            SaveStateChanged?.Invoke(AnythingToSave);
        }

        // Functions
        public void AddSave<T>(object param = null) where T : SaveContext, new()
        {
            var context = GetSaveContext<T>(param);
            if(!context.ShouldSave)
            {
                context.ShouldSave = true;

                NotifySaveStateChanged();
            }
        }

        public async void SaveNow<T>(Project project, object param = null) where T : SaveContext, new()
        {
            var context = GetSaveContext<T>(param);

            SaveContexts.Remove(context.GetType());
            await context.SaveAction(project);
            if (project != null)
                ProjectSaveState.Record(project);
            NotifySaveStateChanged();
        }

        public bool SaveContextExists<T>() where T : SaveContext, new()
        {
            return SaveContexts.ContainsKey(typeof(T));
        }

        public T GetSaveContext<T>(object param = null) where T : SaveContext, new()
        {
            if (!SaveContextExists<T>())
            {
                var newContext = new T();
                SaveContexts.Add(typeof(T), newContext);
                newContext.Init(ResourceModel, param);
            }
            return SaveContexts[typeof(T)] as T;
        }

        public async Task Save(Project project)
        {
            await SaveGate.WaitAsync();
            try
            {
                while (true)
                {
                    var pending = SaveContexts.Values.Where(context => context.ShouldSave).ToList();
                    if (pending.Count == 0)
                        break;

                    foreach (var saveContext in pending)
                    {
                        saveContext.ShouldSave = false;
                        try
                        {
                            await saveContext.SaveAction(project);
                        }
                        catch
                        {
                            saveContext.ShouldSave = true;
                            throw;
                        }
                    }
                }

                try
                {
                    if (project != null)
                        ProjectSaveState.Record(project);
                }
                catch
                {
                    foreach (var context in SaveContexts.Values)
                        context.ShouldSave = true;
                    throw;
                }

                SaveContexts.Clear();
                NotifySaveStateChanged();
            }
            finally
            {
                SaveGate.Release();
            }
        }

        public async Task CreateAutosave(Project project, string snapshotPath)
        {
            await SaveGate.WaitAsync();
            try
            {
                var sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(project.Location));
                var targetPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(snapshotPath));
                var relativeTarget = Path.GetRelativePath(sourcePath, targetPath);
                if (!Path.IsPathRooted(relativeTarget) && relativeTarget != ".." &&
                    !relativeTarget.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                    throw new ArgumentException("The autosave snapshot must be outside the project folder.", nameof(snapshotPath));
                if (Directory.Exists(targetPath) || File.Exists(targetPath))
                    throw new IOException($"The autosave snapshot destination already exists: {targetPath}");

                await Task.Run(() => CopyProjectFiles(sourcePath, targetPath));
                var snapshotProject = project.CreateCopyAtLocation(targetPath);
                var pending = SaveContexts.Values.Where(context => context.ShouldSave).ToList();
                foreach (var saveContext in pending)
                    await saveContext.SaveAutosaveAction(project, snapshotProject);
            }
            finally
            {
                SaveGate.Release();
            }
        }

        private static void CopyProjectFiles(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);
            CopyDirectoryContents(sourcePath, sourcePath, targetPath);
        }

        private static void CopyDirectoryContents(string sourceRoot, string sourcePath, string targetPath)
        {
            foreach (var directory in Directory.EnumerateDirectories(sourcePath))
            {
                var attributes = File.GetAttributes(directory);
                var relativePath = Path.GetRelativePath(sourceRoot, directory);
                if ((attributes & FileAttributes.ReparsePoint) != 0 || IsExcludedDirectory(relativePath))
                    continue;

                var targetDirectory = Path.Combine(targetPath, Path.GetFileName(directory));
                Directory.CreateDirectory(targetDirectory);
                CopyDirectoryContents(sourceRoot, directory, targetDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(sourcePath))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    continue;

                var targetFile = Path.Combine(targetPath, Path.GetFileName(file));
                File.Copy(file, targetFile, overwrite: true);
                File.SetLastWriteTimeUtc(targetFile, File.GetLastWriteTimeUtc(file));
            }
        }

        private static bool IsExcludedDirectory(string relativePath)
        {
            var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return parts.Any(part => part.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || part.Equals(".autosave-settings", StringComparison.OrdinalIgnoreCase)
                || part.Equals(".vs", StringComparison.OrdinalIgnoreCase)
                || part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || part.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                || (parts.Length >= 2 && parts[0].Equals("TypeO", StringComparison.OrdinalIgnoreCase)
                    && parts[1].Equals("build", StringComparison.OrdinalIgnoreCase));
        }
    }
}
