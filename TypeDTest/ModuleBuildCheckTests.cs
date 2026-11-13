using System.Reflection;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;
using Module = TypeD.Models.Data.Module;

namespace TypeDTest;

public class ModuleBuildCheckTests
{
    [Fact]
    public async Task BuildReportsMissingModuleOutputWhenSolutionOnlyBuildsOtherProjects()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-missing-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(directory, "Other"));
        File.WriteAllText(Path.Combine(directory, "Other", "Other.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(directory, "Mod.slnx"),
            """<Solution><Project Path="Other/Other.csproj" /></Solution>""");
        var resources = InitializeResources();
        var log = new List<string>();
        resources.Get<ILogModel>().AttachLogOutput("test", log.Add);
        var module = new Module { Name = "Mod", Version = $"{directory};local" };
        try
        {
            Assert.False(await resources.Get<IModuleModel>().Build(module));

            Assert.Contains(log, line => line.Contains("build did not produce the expected assembly") && line.Contains("Mod.dll"));
            Assert.Contains(log, line => line.Contains("Build succeeded"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task BuildReturnsFalseWhenCompilationFailsEvenWithOldOutput()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-failed-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(directory, "Mod", "bin", "Debug", "net10.0"));
        var resources = InitializeResources();
        var module = new Module { Name = "Mod", Version = $"{directory};local" };
        try
        {
            WriteBrokenModule(directory);
            File.WriteAllText(Path.Combine(directory, "Mod", "bin", "Debug", "net10.0", "Mod.dll"), "old output");

            Assert.False(await resources.Get<IModuleModel>().Build(module));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveStopsAfterFailedPreparationBeforeLoadingAnyAssemblies(bool dependenciesResolved)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-failed-resolve-{Guid.NewGuid():N}");
        var moduleDirectory = Path.Combine(directory, "Module");
        Directory.CreateDirectory(Path.Combine(moduleDirectory, "Mod"));
        WriteBrokenModule(moduleDirectory);
        var project = CreateProject(directory);
        var typer = DispatchProxy.Create<ITyperModel, TestModelProxy>();
        ((TestModelProxy)typer).Handler = (method, args) => method.Name switch
        {
            nameof(ITyperModel.Resolve) => Task.FromResult(dependenciesResolved),
            nameof(ITyperModel.ListDependencies) => Task.FromResult<IEnumerable<ModuleDependency>>(new[]
            {
                new ModuleDependency { Name = "Mod", Author = "Test", LocalPath = moduleDirectory },
                new ModuleDependency { Name = "Later", Author = "Test", LocalPath = Path.Combine(directory, "Later") }
            }),
            _ => throw new NotSupportedException(method.Name)
        };
        var resources = InitializeResources(typer);
        var updates = new List<ProgressUpdate>();
        var log = new List<string>();
        resources.Get<ILogModel>().AttachLogOutput("test", log.Add);
        try
        {
            Assert.False(await resources.Get<IModuleModel>().Resolve(project, updates.Add));

            Assert.Contains(log, line => line.Contains(dependenciesResolved
                ? "Mod failed to build" : "Could not resolve dependencies"));
            Assert.DoesNotContain(updates, update => update.Message == "Loading modules" ||
                update.SubTask?.Contains("Building Later") == true);
            Assert.DoesNotContain(updates, update => update.Message == "Modules ready");
            Assert.All(project.Modules, module =>
                Assert.Null(typeof(Module).GetProperty("Assembly", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(module)));
            Assert.False(File.Exists(Path.Combine(project.ProjectTypeOPath, "cache", "modules.json")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProjectLoadStartsWatchingOnlyAfterSuccessfulPreparation(bool modulesReady)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-opening-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var previousDirectory = Environment.CurrentDirectory;
        var projectPath = Path.Combine(directory, "Example.typeo");
        File.WriteAllText(projectPath, """
            {"ProjectName":"Example","CSProjName":"Example","CSSolutionPath":"Example.sln","FormatVersions":{"TypeD":1}}
            """);
        var calls = new List<string>();
        var projects = DispatchProxy.Create<IProjectModel, TestModelProxy>();
        ((TestModelProxy)projects).Handler = (method, args) =>
        {
            calls.Add(method.Name);
            return method.Name == nameof(IProjectModel.LoadAssembly) ? true : null;
        };
        var modules = DispatchProxy.Create<IModuleModel, TestModelProxy>();
        ((TestModelProxy)modules).Handler = (method, args) =>
        {
            Assert.Equal(nameof(IModuleModel.Resolve), method.Name);
            Assert.DoesNotContain(nameof(IProjectModel.StartWatching), calls);
            return Task.FromResult(modulesReady);
        };
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Remove("ProjectModel");
        resources.Add(projects);
        resources.Remove("ModuleModel");
        resources.Add(modules);
        resources.Get<ISettingModel>().Init(resources);
        resources.Get<IRestoreModel>().AddRestoreMethod(_ =>
            Assert.DoesNotContain(nameof(IProjectModel.StartWatching), calls));
        var provider = resources.Get<IProjectProvider>();
        provider.Init(resources);
        try
        {
            if (modulesReady)
            {
                var loaded = await provider.Load(projectPath, _ => { }, regenerateComponentCode: false);

                Assert.NotNull(loaded);
                Assert.Equal(new[]
                {
                    nameof(IProjectModel.LoadAssembly),
                    nameof(IProjectModel.BuildComponentTree),
                    nameof(IProjectModel.StartWatching)
                }, calls);
            }
            else
            {
                var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.Load(projectPath, _ => { }));

                Assert.Contains("Could not resolve or build project modules", failure.Message);
                Assert.DoesNotContain(nameof(IProjectModel.StartWatching), calls);
                Assert.DoesNotContain(nameof(IProjectModel.LoadAssembly), calls);
                Assert.DoesNotContain(nameof(IProjectModel.BuildComponentTree), calls);
            }
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void IsBuiltRequiresThePackageAssembliesNextToTheModuleAssembly()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeD-built-{Guid.NewGuid():N}");
        var output = Path.Combine(directory, "Mod", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(output);
        try
        {
            var module = new Module { Name = "Mod", Version = $"{directory};local" };
            File.WriteAllText(Path.Combine(output, "Mod.dll"), "");

            // Without a deps file only the module assembly is required
            Assert.True(IsBuilt(module));

            File.WriteAllText(Path.Combine(output, "Mod.deps.json"), """
                {
                  "targets": {
                    ".NETCoreApp,Version=v10.0": {
                      "Mod/1.0.0": { "runtime": { "Mod.dll": {} } },
                      "Pkg/1.0.0": { "runtime": { "lib/net10.0/Pkg.dll": {}, "lib/net10.0/_._": {} } },
                      "Other/2.0.0": { "compile": { "lib/net10.0/Other.dll": {} } }
                    }
                  },
                  "libraries": {
                    "Mod/1.0.0": { "type": "project" },
                    "Pkg/1.0.0": { "type": "package" },
                    "Other/2.0.0": { "type": "package" }
                  }
                }
                """);

            // A build outside the editor leaves the NuGet assemblies out of the output, so the module is not built
            Assert.False(IsBuilt(module));

            File.WriteAllText(Path.Combine(output, "Pkg.dll"), "");
            Assert.True(IsBuilt(module));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static bool IsBuilt(Module module)
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var model = resources.Get<IModuleModel>();
        var method = model.GetType().GetMethod("IsBuilt", BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)method.Invoke(model, new object[] { module });
    }

    private static TestResourceModel InitializeResources(ITyperModel typer = null)
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        if (typer != null)
        {
            resources.Remove("TyperModel");
            resources.Add(typer);
        }
        resources.Get<IModuleModel>().Init(resources);
        return resources;
    }

    private static void WriteBrokenModule(string directory)
    {
        File.WriteAllText(Path.Combine(directory, "Mod", "Mod.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(directory, "Mod", "Broken.cs"), "namespace Mod; public class Broken { int }");
    }

    private static Project CreateProject(string directory)
    {
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dtoType = constructor.GetParameters()[1].ParameterType;
        var dto = Activator.CreateInstance(dtoType);
        dtoType.GetProperty("ProjectName").SetValue(dto, "Example");
        dtoType.GetProperty("CSProjName").SetValue(dto, "Example");
        dtoType.GetProperty("CSSolutionPath").SetValue(dto, "Example.sln");
        return (Project)constructor.Invoke(new object[] { directory, dto });
    }

    public class TestModelProxy : DispatchProxy
    {
        public Func<MethodInfo, object[], object> Handler { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args);
    }
}
