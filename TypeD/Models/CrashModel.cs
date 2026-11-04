using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using Module = TypeD.Models.Data.Module;

namespace TypeD.Models
{
    internal class CrashModel : ICrashModel
    {
        private const int MaxLogLines = 500;

        // Models
        ILogModel LogModel { get; set; }
        IHookModel HookModel { get; set; }
        IModuleModel ModuleModel { get; set; }
        IProjectModel ProjectModel { get; set; }

        // Properties
        public Project CurrentProject { get; set; }
        public event Action<CrashReport> Crashed;

        private readonly object LogLock = new();
        private readonly Queue<string> LogLines = new();

        // Constructors
        public CrashModel() { }

        public void Init(IResourceModel resourceModel)
        {
            LogModel = resourceModel.Get<ILogModel>();
            HookModel = resourceModel.Get<IHookModel>();
            ModuleModel = resourceModel.Get<IModuleModel>();
            ProjectModel = resourceModel.Get<IProjectModel>();
            LogModel.AttachLogOutput("CrashModel", line => Capture($"[{DateTime.Now:HH:mm:ss}] {line}"));
            // The game hosted inside the editor logs to the console, so capture that output as well
            if (Console.Out is not CaptureWriter)
                Console.SetOut(new CaptureWriter(Console.Out, Capture));
        }

        // Functions
        public CrashReport Identify(Exception exception)
        {
            var project = CurrentProject;
            if (project == null || exception == null) return null;

            var gameCrashed = false;
            var crashedModules = new List<string>();
            foreach (var assembly in OriginsOf(exception, assembly => IsGame(project, assembly) || FindModule(project, assembly) != null))
            {
                if (IsGame(project, assembly))
                {
                    gameCrashed = true;
                }
                else if (FindModule(project, assembly) is Module module && !crashedModules.Contains(module.Name))
                {
                    crashedModules.Add(module.Name);
                }
            }

            if (!gameCrashed && crashedModules.Count == 0) return null;

            return new CrashReport()
            {
                Project = project,
                Exception = exception,
                GameCrashed = gameCrashed,
                CrashedModules = crashedModules,
                LogOutput = GetCapturedLog()
            };
        }

        public bool Report(Exception exception)
        {
            var report = Identify(exception);
            if (report == null) return false;

            StopGame(report.Project);
            Crashed?.Invoke(report);
            return true;
        }

        public void StopGame(Project project)
        {
            HookModel.Shoot(new GameStopHook() { Project = project });
        }

        public async Task<bool> Reload(CrashReport report, Action<string> progress = null)
        {
            var project = report.Project;
            try
            {
                progress?.Invoke("Stopping the game");
                StopGame(project);

                // Loading a module again restarts its editor side and resets its state
                var crashedModules = project.Modules.Where(m => report.CrashedModules.Contains(m.Name)).ToList();
                if (crashedModules.Count > 0)
                {
                    progress?.Invoke($"Reloading {string.Join(", ", crashedModules.Select(m => m.Name))}");
                    ModuleModel.ReloadAssemblies(project, crashedModules, (module, _, _) => LogModel.Log($"Reloading module {module.Name}"));
                }

                progress?.Invoke("Rebuilding and loading the game");
                LogModel.Log("Reloading the game");
                if (!await ProjectModel.Build(project))
                {
                    LogModel.Log("The game could not be reloaded since the project failed to build");
                    return false;
                }

                progress?.Invoke("Starting the game");
                HookModel.Shoot(new GameStartHook() { Project = project });
                return true;
            }
            catch (Exception e)
            {
                LogModel.Log($"Reloading the game failed: {e}");
                return false;
            }
        }

        public string GetCapturedLog()
        {
            lock (LogLock)
            {
                return string.Join(Environment.NewLine, LogLines);
            }
        }

        // Internal functions
        private void Capture(string line)
        {
            if (line == null) return;
            lock (LogLock)
            {
                LogLines.Enqueue(line);
                while (LogLines.Count > MaxLogLines) LogLines.Dequeue();
            }
        }

        private static bool IsGame(Project project, Assembly assembly)
        {
            return assembly == project.Assembly || assembly.GetName().Name == project.CSProjName;
        }

        private static Module FindModule(Project project, Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return project.Modules.Find(m =>
                assembly == m.Assembly || assembly == m.DevAssembly ||
                name == m.Name || (m.HaveDevModule && name == m.TypeO.DevModuleName));
        }

        // The game or module assemblies the crash originated in. The engine calls into the game and modules, so every frame below the
        // throwing code also belongs to them; only the top-most matching frame of the innermost exceptions is where the crash happened.
        private static IEnumerable<Assembly> OriginsOf(Exception exception, Func<Assembly, bool> matches)
        {
            var origins = new List<(int Depth, Assembly Assembly)>();
            var pending = new Queue<(Exception Exception, int Depth)>();
            var visited = new HashSet<Exception>();
            pending.Enqueue((exception, 0));
            while (pending.Count > 0)
            {
                var (current, depth) = pending.Dequeue();
                if (current == null || !visited.Add(current)) continue;

                if (current is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions) pending.Enqueue((inner, depth + 1));
                }
                else if (current.InnerException != null)
                {
                    pending.Enqueue((current.InnerException, depth + 1));
                }

                var assemblies = new StackTrace(current).GetFrames()
                    .Select(frame => frame.GetMethod()?.Module?.Assembly)
                    .Prepend(current.TargetSite?.Module?.Assembly);
                var origin = assemblies.FirstOrDefault(assembly => assembly != null && matches(assembly));
                if (origin != null) origins.Add((depth, origin));
            }

            if (origins.Count == 0) return Enumerable.Empty<Assembly>();
            var deepest = origins.Max(o => o.Depth);
            return origins.Where(o => o.Depth == deepest).Select(o => o.Assembly).Distinct();
        }

        // Writes console output through to the original writer while keeping the most recent lines for crash reports
        private class CaptureWriter : TextWriter
        {
            private readonly TextWriter Inner;
            private readonly Action<string> Capture;
            private readonly StringBuilder Line = new();

            public CaptureWriter(TextWriter inner, Action<string> capture)
            {
                Inner = inner;
                Capture = capture;
            }

            public override Encoding Encoding { get { return Inner.Encoding; } }

            public override void Write(char value)
            {
                Inner.Write(value);
                lock (Line)
                {
                    if (value == '\n')
                    {
                        Capture(Line.ToString().TrimEnd('\r'));
                        Line.Clear();
                    }
                    else
                    {
                        Line.Append(value);
                    }
                }
            }

            public override void Flush()
            {
                Inner.Flush();
            }
        }
    }
}
