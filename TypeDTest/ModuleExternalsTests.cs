using System.Reflection;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using Module = TypeD.Models.Data.Module;

namespace TypeDTest;

public class ModuleExternalsTests
{
    [Fact]
    public void CopyExternals_CopiesGameAndDevExternalsIntoTheProject()
    {
        using var fixture = new ExternalsFixture();
        fixture.ModuleFile(Path.Combine("Mod", "Shaders", "shader.frag"), "frag");
        fixture.ModuleFile(Path.Combine("DevMod", "Assets", "Icons", "tool.svg"), "icon");
        fixture.ModuleFile(Path.Combine("DevMod", "Assets", "Icons", "Sub", "nested.svg"), "nested");
        fixture.ModuleFile(Path.Combine("Mod", "Mock", "texture.png"), "test-only");
        fixture.ModuleFile(Path.Combine("Mod", "Docs", "readme.md"), "other-tag");

        fixture.CopyExternals("Shaders/shader.frag", "Assets/Icons/**;dev", "Mock/texture.png;test", "Docs/readme.md;custom");

        Assert.Equal("frag", fixture.ProjectContent(Path.Combine("Shaders", "shader.frag")));
        Assert.Equal("icon", fixture.ProjectContent(Path.Combine("Assets", "Icons", "tool.svg")));
        Assert.Equal("nested", fixture.ProjectContent(Path.Combine("Assets", "Icons", "Sub", "nested.svg")));
        Assert.False(File.Exists(Path.Combine(fixture.ProjectPath, "Mock", "texture.png")));
        Assert.False(File.Exists(Path.Combine(fixture.ProjectPath, "Docs", "readme.md")));
    }

    [Fact]
    public void CopyExternals_OverwritesOlderFilesAndKeepsNewerOnes()
    {
        using var fixture = new ExternalsFixture();
        var source = fixture.ModuleFile(Path.Combine("Mod", "Shaders", "shader.frag"), "new");

        var stale = fixture.ProjectFile(Path.Combine("Shaders", "shader.frag"), "stale");
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-1));
        fixture.CopyExternals("Shaders/shader.frag");
        Assert.Equal("new", fixture.ProjectContent(Path.Combine("Shaders", "shader.frag")));

        var edited = fixture.ProjectFile(Path.Combine("Shaders", "shader.frag"), "edited");
        File.SetLastWriteTimeUtc(edited, DateTime.UtcNow.AddHours(1));
        fixture.CopyExternals("Shaders/shader.frag");
        Assert.Equal("edited", fixture.ProjectContent(Path.Combine("Shaders", "shader.frag")));
    }

    [Fact]
    public void CopyExternals_FindsFilesInTheModuleRootAsWell()
    {
        using var fixture = new ExternalsFixture();
        fixture.ModuleFile(Path.Combine("Shaders", "root.frag"), "from-root");

        fixture.CopyExternals("Shaders/root.frag");

        Assert.Equal("from-root", fixture.ProjectContent(Path.Combine("Shaders", "root.frag")));
    }

    private sealed class ExternalsFixture : IDisposable
    {
        private readonly string Directory;
        public string ProjectPath { get; }
        private readonly string ModulePath;
        private readonly Module Module;
        private readonly IModuleModel Model;

        public ExternalsFixture()
        {
            Directory = Path.Combine(Path.GetTempPath(), $"TypeD-externals-{Guid.NewGuid():N}");
            ProjectPath = Path.Combine(Directory, "Game");
            ModulePath = Path.Combine(Directory, "ModFolder");
            System.IO.Directory.CreateDirectory(ProjectPath);
            System.IO.Directory.CreateDirectory(ModulePath);

            var resources = new TestResourceModel();
            TypeDInit.Init(resources);
            Model = resources.Get<IModuleModel>();
            Model.Init(resources);

            var typeO = (ModuleTypeO)Activator.CreateInstance(typeof(ModuleTypeO), nonPublic: true);
            typeO.DevModuleName = "DevMod";
            Module = new Module { Name = "Mod", Version = $"{ModulePath};local" };
            typeof(Module).GetProperty(nameof(Module.TypeO)).SetValue(Module, typeO);
        }

        public void CopyExternals(params string[] externals)
        {
            Module.TypeO.Externals = externals.ToList();
            var method = Model.GetType().GetMethod("CopyExternals", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(Model, new object[] { CreateProject(ProjectPath), Module });
        }

        public string ModuleFile(string relativePath, string content)
        {
            var path = Path.Combine(ModulePath, relativePath);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return path;
        }

        public string ProjectFile(string relativePath, string content)
        {
            var path = Path.Combine(ProjectPath, relativePath);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return path;
        }

        public string ProjectContent(string relativePath)
        {
            return File.ReadAllText(Path.Combine(ProjectPath, relativePath));
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

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }
}
