using System.Reflection;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;

public class ProjectBuildTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Build_ProducesLoadableOutputWithoutSolutionOutputWarning(bool includeSolution)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(directory, "Example"));
        Directory.CreateDirectory(Path.Combine(directory, "Dependency"));
        var (model, hooks) = InitializeModels();
        var project = CreateProject(directory);
        BuildStatusChangedHook completed = null;
        hooks.AddHook<BuildStatusChangedHook>(hook =>
        {
            if (hook.IsComplete)
                completed = hook;
        });

        try
        {
            File.WriteAllText(project.ProjectCSProjPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\Dependency\Dependency.csproj" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(directory, "Example", "Program.cs"),
                "System.Console.WriteLine(Dependency.Value.Text);");
            File.WriteAllText(Path.Combine(directory, "Dependency", "Dependency.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(directory, "Dependency", "Value.cs"),
                """namespace Dependency; public static class Value { public const string Text = "Hello"; }""");
            if (includeSolution)
            {
                File.WriteAllText(Path.Combine(directory, "Example.slnx"), """
                    <Solution>
                      <Project Path="Example/Example.csproj" />
                      <Project Path="Dependency/Dependency.csproj" />
                    </Solution>
                    """);
            }

            var succeeded = await model.Build(project);

            Assert.NotNull(completed);
            Assert.True(succeeded, string.Join(Environment.NewLine, completed.Errors.Select(error => error.Message)));
            Assert.True(completed.Succeeded);
            Assert.Empty(completed.Warnings);
            Assert.NotNull(project.Assembly);
            foreach (var file in new[] { "Example.dll", "Example.exe", "Example.deps.json", "Example.runtimeconfig.json", "Dependency.dll" })
                Assert.True(File.Exists(Path.Combine(project.ProjectBuildOutputPath, file)), $"Missing output: {file}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Build_ReportsMissingProjectFile()
    {
        var (model, hooks) = InitializeModels();
        var project = CreateProject(Path.Combine(Path.GetTempPath(), $"TypeD-missing-{Guid.NewGuid():N}"));
        BuildStatusChangedHook completed = null;
        hooks.AddHook<BuildStatusChangedHook>(hook => completed = hook);

        Assert.False(await model.Build(project));

        Assert.True(completed.IsComplete);
        Assert.False(completed.Succeeded);
        Assert.Contains(project.ProjectCSProjPath, Assert.Single(completed.Errors).Message);
    }

    [Fact]
    public async Task Build_RebuildsAndReloadsChangedModuleAndAttributesIssues()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-modules-{Guid.NewGuid():N}");
        var resources = InitializeResources();
        var model = resources.Get<IProjectModel>();
        var hooks = resources.Get<IHookModel>();
        var (project, module) = CreateProjectWithModule(directory);
        var updates = new List<BuildStatusChangedHook>();
        var stopped = false;
        var started = false;
        hooks.AddHook<BuildStatusChangedHook>(hook => { lock (updates) updates.Add(hook); });
        hooks.AddHook<GameStopHook>(_ => stopped = true);
        hooks.AddHook<GameStartHook>(_ => started = true);

        try
        {
            var succeeded = await model.Build(project, new[] { module });

            var completed = updates.Last();
            Assert.True(succeeded, string.Join(Environment.NewLine, completed.Errors));
            Assert.True(completed.IsComplete);

            // Each warning is reported once and tagged with the project it comes from
            var moduleWarning = Assert.Single(completed.Warnings, w => w.Message.Contains("module warning"));
            Assert.True(moduleWarning.IsModuleIssue);
            Assert.Equal("Mod", moduleWarning.Source);
            Assert.DoesNotContain("[", moduleWarning.Message);
            var gameWarning = Assert.Single(completed.Warnings, w => w.Message.Contains("game warning"));
            Assert.False(gameWarning.IsModuleIssue);
            Assert.Equal("Example", gameWarning.Source);

            // The progress moves forward through the module build, the game build and the reload
            var progress = updates.Where(u => u.IsBuilding).ToList();
            Assert.Contains(progress, u => u.Status == "Building module Mod" && u.SubTask != null);
            Assert.Contains(progress, u => u.Status == "Building Example");
            Assert.Contains(progress, u => u.Status == "Reloading modules");
            Assert.Equal(progress.Select(u => u.Progress).OrderBy(p => p), progress.Select(u => u.Progress));

            // The module is loaded from memory, so its file stays free to be rebuilt
            Assert.NotNull(LoadedAssembly(module));
            Assert.NotNull(project.Assembly);
            Assert.True(stopped);
            Assert.True(started);
            var moduleDll = Directory.GetFiles(Path.Combine(directory, "Mod", "Mod", "bin"), "Mod.dll", SearchOption.AllDirectories).Single();
            using (File.Open(moduleDll, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }

            // The game binds to the reloaded module
            var firstModuleAssembly = LoadedAssembly(module);
            Assert.True(await model.Build(project, new[] { module }));
            Assert.NotSame(firstModuleAssembly, LoadedAssembly(module));
            var gameValue = project.Assembly.GetType("Example.Game").GetMethod("Value").Invoke(null, null);
            Assert.Equal("Hello", gameValue);
            Assert.Same(LoadedAssembly(module), project.Assembly.GetType("Example.Game").GetMethod("ModuleAssembly").Invoke(null, null));
        }
        finally
        {
            foreach (var loaded in project.Modules)
                resources.Get<IModuleModel>().UnloadAssembly(loaded);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Build_StopsWhenChangedModuleFailsToBuild()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-modules-{Guid.NewGuid():N}");
        var resources = InitializeResources();
        var model = resources.Get<IProjectModel>();
        var (project, module) = CreateProjectWithModule(directory);
        File.WriteAllText(Path.Combine(directory, "Mod", "Mod", "Broken.cs"), "namespace Mod; public class Broken { int }");
        BuildStatusChangedHook completed = null;
        resources.Get<IHookModel>().AddHook<BuildStatusChangedHook>(hook => { if (hook.IsComplete) completed = hook; });

        try
        {
            Assert.False(await model.Build(project, new[] { module }));

            Assert.Equal("Build failed in module Mod", completed.Status);
            Assert.NotEmpty(completed.Errors);
            Assert.All(completed.Errors, error => Assert.True(error.IsModuleIssue && error.Source == "Mod", error.ToString()));
            Assert.Null(project.Assembly);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Watching_RebuildsModuleWhenItsSourceChanges()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-modules-{Guid.NewGuid():N}");
        var resources = InitializeResources();
        var model = resources.Get<IProjectModel>();
        var (project, module) = CreateProjectWithModule(directory);
        var moduleBuilt = new TaskCompletionSource<BuildStatusChangedHook>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sawModuleStep = false;
        resources.Get<IHookModel>().AddHook<BuildStatusChangedHook>(hook =>
        {
            if (hook.IsBuilding && hook.Status == "Building module Mod")
                sawModuleStep = true;
            if (hook.IsComplete && sawModuleStep)
                moduleBuilt.TrySetResult(hook);
        });

        model.StartWatching(project);
        try
        {
            File.AppendAllText(Path.Combine(directory, "Mod", "Mod", "Value.cs"), Environment.NewLine + "// changed");

            var completed = await moduleBuilt.Task.WaitAsync(TimeSpan.FromMinutes(2));
            Assert.True(completed.Succeeded, string.Join(Environment.NewLine, completed.Errors));
            Assert.NotNull(LoadedAssembly(module));
        }
        finally
        {
            model.StopWatching(project);
            foreach (var loaded in project.Modules)
                resources.Get<IModuleModel>().UnloadAssembly(loaded);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static (Project Project, TypeD.Models.Data.Module Module) CreateProjectWithModule(string directory)
    {
        var moduleDirectory = Path.Combine(directory, "Mod");
        Directory.CreateDirectory(Path.Combine(moduleDirectory, "Mod"));
        Directory.CreateDirectory(Path.Combine(directory, "Game", "Example"));
        var project = CreateProject(Path.Combine(directory, "Game"));
        var module = new TypeD.Models.Data.Module { Name = "Mod", Version = $"{moduleDirectory};local" };
        project.Modules.Add(module);

        File.WriteAllText(Path.Combine(moduleDirectory, "Mod", "Mod.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(moduleDirectory, "Mod", "Value.cs"),
            "namespace Mod; public static class Value { public static string Text() => \"Hello\"; }\n#warning module warning");
        File.WriteAllText(project.ProjectCSProjPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{Path.Combine(moduleDirectory, "Mod", "Mod.csproj")}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(directory, "Game", "Example", "Game.cs"), """
            namespace Example;
            public static class Game
            {
                public static string Value() => Mod.Value.Text();
                public static System.Reflection.Assembly ModuleAssembly() => typeof(Mod.Value).Assembly;
            }
            #warning game warning
            """);
        return (project, module);
    }

    private static System.Reflection.Assembly LoadedAssembly(TypeD.Models.Data.Module module) =>
        (System.Reflection.Assembly)typeof(TypeD.Models.Data.Module)
            .GetProperty("Assembly", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(module);

    private static TestResourceModel InitializeResources()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<IModuleModel>().Init(resources);
        resources.Get<IProjectModel>().Init(resources);
        return resources;
    }

    private static (IProjectModel Model, IHookModel Hooks) InitializeModels()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var model = resources.Get<IProjectModel>();
        model.Init(resources);
        return (model, resources.Get<IHookModel>());
    }

    private static Project CreateProject(string directory)
    {
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dtoType = constructor.GetParameters()[1].ParameterType;
        var dto = Activator.CreateInstance(dtoType);
        dtoType.GetProperty("ProjectName").SetValue(dto, "Example");
        dtoType.GetProperty("CSProjName").SetValue(dto, "Example");
        dtoType.GetProperty("CSSolutionPath").SetValue(dto, "Example.sln");
        return (Project)constructor.Invoke(new[] { directory, dto });
    }
}
