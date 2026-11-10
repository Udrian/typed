using System.Reflection;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;

namespace TypeDTest;

public class CrashModelTests
{
    [Fact]
    public void Identify_ReturnsNullWithoutProjectOrException()
    {
        var (crash, _) = InitializeModels();

        Assert.Null(crash.Identify(Thrown()));

        crash.CurrentProject = CreateProject("Example");
        Assert.Null(crash.Identify(null));
    }

    [Fact]
    public void Identify_ReturnsNullForUnrelatedExceptions()
    {
        var (crash, _) = InitializeModels();
        crash.CurrentProject = CreateProject("Example");

        Assert.Null(crash.Identify(Thrown()));
    }

    [Fact]
    public void Identify_ReportsGameCrashByAssemblyName()
    {
        var (crash, _) = InitializeModels();
        // The game assembly is named after the C# project, which here is the test assembly itself
        crash.CurrentProject = CreateProject(typeof(CrashModelTests).Assembly.GetName().Name);

        var report = crash.Identify(Thrown());

        Assert.NotNull(report);
        Assert.True(report.GameCrashed);
        Assert.Empty(report.CrashedModules);
        Assert.Same(crash.CurrentProject, report.Project);
    }

    [Fact]
    public void Identify_ReportsModuleCrashByAssemblyName()
    {
        var (crash, _) = InitializeModels();
        var project = CreateProject("Example");
        project.Modules.Add(new TypeD.Models.Data.Module() { Name = typeof(CrashModelTests).Assembly.GetName().Name, Version = "1.0.0" });
        crash.CurrentProject = project;

        var report = crash.Identify(Thrown());

        Assert.NotNull(report);
        Assert.False(report.GameCrashed);
        Assert.Equal(typeof(CrashModelTests).Assembly.GetName().Name, Assert.Single(report.CrashedModules));
    }

    [Fact]
    public void Identify_FindsCrashInInnerExceptions()
    {
        var (crash, _) = InitializeModels();
        crash.CurrentProject = CreateProject(typeof(CrashModelTests).Assembly.GetName().Name);

        var report = crash.Identify(new AggregateException(new InvalidOperationException("outer", Thrown())));

        Assert.NotNull(report);
        Assert.True(report.GameCrashed);
    }

    [Fact]
    public void Identify_BlamesTopMostFrameOfGameOrModule()
    {
        var (crash, _) = InitializeModels();
        var project = CreateProject(typeof(CrashModelTests).Assembly.GetName().Name);
        crash.CurrentProject = project;

        // Thrown from CoreLib, called by the game: the game is blamed when CoreLib is not a module
        var report = crash.Identify(ThrownByCoreLib());
        Assert.True(report.GameCrashed);
        Assert.Empty(report.CrashedModules);

        // When CoreLib is a module it is the top-most frame, so only the module is blamed
        project.Modules.Add(new TypeD.Models.Data.Module() { Name = typeof(int).Assembly.GetName().Name, Version = "1.0.0" });
        report = crash.Identify(ThrownByCoreLib());
        Assert.False(report.GameCrashed);
        Assert.Equal(typeof(int).Assembly.GetName().Name, Assert.Single(report.CrashedModules));
    }

    [Fact]
    public void Report_StopsGameAndRaisesCrashed()
    {
        var (crash, _, hooks) = InitializeModelsWithHooks();
        crash.CurrentProject = CreateProject(typeof(CrashModelTests).Assembly.GetName().Name);
        GameStopHook stopped = null;
        CrashReport crashed = null;
        hooks.AddHook<GameStopHook>(hook => stopped = hook);
        crash.Crashed += report => crashed = report;

        Assert.True(crash.Report(Thrown()));

        Assert.Same(crash.CurrentProject, stopped?.Project);
        Assert.True(crashed?.GameCrashed);
    }

    [Fact]
    public void Report_IgnoresUnrelatedExceptions()
    {
        var (crash, _, hooks) = InitializeModelsWithHooks();
        crash.CurrentProject = CreateProject("Example");
        var stopped = false;
        var crashed = false;
        hooks.AddHook<GameStopHook>(_ => stopped = true);
        crash.Crashed += _ => crashed = true;

        Assert.False(crash.Report(Thrown()));

        Assert.False(stopped);
        Assert.False(crashed);
    }

    [Fact]
    public async Task Reload_DoesNotStartGameWhenBuildFails()
    {
        var (crash, _, hooks) = InitializeModelsWithHooks();
        crash.CurrentProject = CreateProject(typeof(CrashModelTests).Assembly.GetName().Name);
        var report = crash.Identify(Thrown());
        var stopped = false;
        var started = false;
        hooks.AddHook<GameStopHook>(_ => stopped = true);
        hooks.AddHook<GameStartHook>(_ => started = true);

        // The project has no C# project on disk, so it can never build
        Assert.False(await crash.Reload(report));

        Assert.True(stopped);
        Assert.False(started);
    }

    [Fact]
    public void GetCapturedLog_CapturesLogAndConsoleOutput()
    {
        var (crash, log) = InitializeModels();
        var marker = $"marker-{Guid.NewGuid():N}";

        log.Log($"log {marker}");
        Console.WriteLine($"game {marker}");

        var captured = crash.GetCapturedLog();
        Assert.Contains($"log {marker}", captured);
        Assert.Contains($"game {marker}", captured);
    }

    private static Exception Thrown()
    {
        try
        {
            throw new InvalidOperationException("The game did something impossible");
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static Exception ThrownByCoreLib()
    {
        try
        {
            int.Parse("not a number");
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static (ICrashModel Crash, ILogModel Log, IHookModel Hooks) InitializeModelsWithHooks()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var crash = resources.Get<ICrashModel>();
        crash.Init(resources);
        return (crash, resources.Get<ILogModel>(), resources.Get<IHookModel>());
    }

    private static (ICrashModel Crash, ILogModel Log) InitializeModels()
    {
        var (crash, log, _) = InitializeModelsWithHooks();
        return (crash, log);
    }

    internal static Project CreateProject(string csProjName)
    {
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dtoType = constructor.GetParameters()[1].ParameterType;
        var dto = Activator.CreateInstance(dtoType);
        dtoType.GetProperty("ProjectName").SetValue(dto, "Example");
        dtoType.GetProperty("CSProjName").SetValue(dto, csProjName);
        dtoType.GetProperty("CSSolutionPath").SetValue(dto, "Example.sln");
        return (Project)constructor.Invoke(new object[] { Path.Combine(Path.GetTempPath(), $"TypeD-crash-{Guid.NewGuid():N}"), dto });
    }
}
