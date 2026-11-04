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
