using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using TypeD;

namespace TypeDTest;

public class ModuleLoadContextTests
{
    [Fact]
    public void Dependencies_ShareAssemblyAndBindingStateAcrossModuleContexts()
    {
        using var dependency = new DependencyFixture();
        var first = CreateContext("first", dependency.Folder);
        var second = CreateContext("second", dependency.Folder);

        var firstAssembly = first.LoadFromAssemblyName(dependency.Name);
        firstAssembly.GetType("BindingState").GetField("Initialized").SetValue(null, true);
        var secondAssembly = second.LoadFromAssemblyName(dependency.Name);

        Assert.Same(firstAssembly, secondAssembly);
        Assert.True((bool)secondAssembly.GetType("BindingState").GetField("Initialized").GetValue(null));
    }

    [Fact]
    public void Dependencies_ShareOneInstanceAcrossRequestedVersions()
    {
        using var dependency = new DependencyFixture();
        var first = CreateContext("first", dependency.Folder);
        var second = CreateContext("second", dependency.Folder);

        // Different assemblies are often compiled against different versions of the same dependency
        // (e.g. OpenTKAvalonia against OpenTK 4.8.2 and a module against OpenTK 4.9.4); both must
        // bind the same instance, since its static state (e.g. GL bindings) is initialized only once
        var firstAssembly = first.LoadFromAssemblyName(new AssemblyName($"{dependency.Name.Name}, Version=4.8.2.0"));
        var secondAssembly = second.LoadFromAssemblyName(new AssemblyName($"{dependency.Name.Name}, Version=4.9.4.0"));

        Assert.Same(firstAssembly, secondAssembly);
    }

    [Fact]
    public void ModuleAssemblies_StillLoadSeparateInstancesForReloads()
    {
        using var dependency = new DependencyFixture();
        var first = CreateContext("first-module", dependency.Folder);
        var second = CreateContext("reloaded-module", dependency.Folder);

        var firstAssembly = LoadFile(first, dependency.Path);
        var secondAssembly = LoadFile(second, dependency.Path);

        Assert.NotSame(firstAssembly, secondAssembly);
        Assert.Same(first, AssemblyLoadContext.GetLoadContext(firstAssembly));
        Assert.Same(second, AssemblyLoadContext.GetLoadContext(secondAssembly));
    }

    [Fact]
    public void RegisteredModule_TakesPriorityOverSharedDependency()
    {
        using var dependency = new DependencyFixture();
        var original = CreateContext("original", dependency.Folder).LoadFromAssemblyName(dependency.Name);
        var latest = LoadFile(CreateContext("latest-module", dependency.Folder), dependency.Path);
        var consumer = CreateContext("consumer", dependency.Folder, name => name.Name == dependency.Name.Name ? latest : null);

        Assert.NotSame(original, latest);
        Assert.Same(latest, consumer.LoadFromAssemblyName(dependency.Name));
    }

    private static AssemblyLoadContext CreateContext(string name, string folder, Func<AssemblyName, Assembly> resolveModule = null)
    {
        var type = typeof(TypeDInit).Assembly.GetType("TypeD.Models.ModuleLoadContext", throwOnError: true);
        return (AssemblyLoadContext)Activator.CreateInstance(type,
            new object[] { name, folder, resolveModule ?? (Func<AssemblyName, Assembly>)(_ => null) });
    }

    private static Assembly LoadFile(AssemblyLoadContext context, string path) =>
        (Assembly)context.GetType().GetMethod("LoadFile").Invoke(context, new object[] { path });

    private sealed class DependencyFixture : IDisposable
    {
        public string Folder { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ModuleDependencyTests-" + Guid.NewGuid().ToString("N"));
        public AssemblyName Name { get; } = new("TestBindingDependency-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(Folder, Name.Name + ".dll");

        public DependencyFixture()
        {
            Directory.CreateDirectory(Folder);
            var assembly = new PersistedAssemblyBuilder(Name, typeof(object).Assembly);
            var type = assembly.DefineDynamicModule(Name.Name).DefineType("BindingState", TypeAttributes.Public);
            type.DefineField("Initialized", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
            type.CreateType();
            assembly.Save(Path);
        }

        public void Dispose() => Directory.Delete(Folder, recursive: true);
    }
}