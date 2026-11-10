using System.Reflection;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using Module = TypeD.Models.Data.Module;

namespace TypeDTest;

public class ModuleInitializationOrderTests
{
    [Fact]
    public void InitializationOrderPutsDependenciesFirstAndKeepsIndependentOrder()
    {
        var project = CreateProject();
        project.Modules.Add(CreateModule("A", "Udrian@B-0.1.0.dev"));
        project.Modules.Add(CreateModule("B", "Udrian@C-0.1.0", "OpenTK-4.9.4;nuget", "Udrian@TypeD-0.1.0;dev"));
        project.Modules.Add(CreateModule("C"));
        project.Modules.Add(CreateModule("D"));

        var order = InitializationOrder(project);

        Assert.Equal(new[] { "C", "B", "A", "D" }, order.Select(m => m.Name));
    }

    [Fact]
    public void InitializationOrderToleratesCyclesAndUnknownDependencies()
    {
        var project = CreateProject();
        project.Modules.Add(CreateModule("A", "Udrian@B-0.1.0", "Udrian@Missing-1.0.0"));
        project.Modules.Add(CreateModule("B", "Udrian@A-0.1.0"));

        var order = InitializationOrder(project);

        Assert.Equal(new[] { "B", "A" }, order.Select(m => m.Name));
    }

    [Fact]
    public void UnloadAssembliesUninitializesInReverseInitializationOrder()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var model = resources.Get<IModuleModel>();
        model.Init(resources);

        var log = new List<string>();
        var project = CreateProject();
        project.Modules.Add(CreateLoadedModule("A", log, "Udrian@B-0.1.0"));
        project.Modules.Add(CreateLoadedModule("B", log, "Udrian@C-0.1.0"));
        project.Modules.Add(CreateLoadedModule("C", log));

        model.UnloadAssemblies(project);

        Assert.Equal(new[] { "-A", "-B", "-C" }, log);
        Assert.All(project.Modules, m => Assert.False(ModuleInitialized(m)));
    }

    private static List<Module> InitializationOrder(Project project)
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var model = resources.Get<IModuleModel>();
        var method = model.GetType().GetMethod("InitializationOrder", BindingFlags.Static | BindingFlags.NonPublic);
        return (List<Module>)method.Invoke(null, new object[] { project });
    }

    private static Module CreateModule(string name, params string[] dependencies)
    {
        var typeO = (ModuleTypeO)Activator.CreateInstance(typeof(ModuleTypeO), nonPublic: true);
        typeO.Dependencies.AddRange(dependencies);
        var module = new Module { Name = name, Version = "0.1.0" };
        typeof(Module).GetProperty(nameof(Module.TypeO)).SetValue(module, typeO);
        return module;
    }

    private static Module CreateLoadedModule(string name, List<string> log, params string[] dependencies)
    {
        var module = CreateModule(name, dependencies);
        typeof(Module).GetProperty("DevAssembly", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(module, typeof(ModuleInitializationOrderTests).Assembly);
        typeof(Module).GetProperty("TypeDModuleInitializer", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(module, new RecordingInitializer(log, name));
        typeof(Module).GetProperty("Initialized", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(module, true);
        return module;
    }

    private static bool ModuleInitialized(Module module)
    {
        return (bool)typeof(Module).GetProperty("Initialized", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(module);
    }

    private static Project CreateProject()
    {
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dtoType = constructor.GetParameters()[1].ParameterType;
        var dto = Activator.CreateInstance(dtoType);
        dtoType.GetProperty("ProjectName").SetValue(dto, "Example");
        dtoType.GetProperty("CSProjName").SetValue(dto, "Example");
        dtoType.GetProperty("CSSolutionPath").SetValue(dto, "Example.sln");
        return (Project)constructor.Invoke(new object[] { Path.Combine(Path.GetTempPath(), $"TypeD-order-{Guid.NewGuid():N}"), dto });
    }

    private sealed class RecordingInitializer : TypeDModuleInitializer
    {
        private readonly List<string> Log;
        private readonly string Name;

        public RecordingInitializer(List<string> log, string name)
        {
            Log = log;
            Name = name;
        }

        public override void Initializer(Project project) => Log.Add($"+{Name}");
        public override void Uninitializer() => Log.Add($"-{Name}");
    }
}
