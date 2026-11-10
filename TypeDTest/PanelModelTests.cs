using System.Reflection;
using System.Runtime.Loader;
using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.View.Viewer;

namespace TypeDTest;

public class PanelModelTests
{
    public sealed class TestViewer : IViewer
    {
        public Project Project { get; set; }
        public Component Component => null;
        public void Init() { }
        public void Load(Component component) { }
        public void Unload() { }
    }

    // A reloaded module registers the same viewer as a new Type instance; the stale entry must not
    // stay registered, since creating a viewer from it would run code from the unloaded assemblies
    [Fact]
    public void AddViewer_ReplacesViewerFromPreviousAssemblyGeneration()
    {
        var panelModel = CreatePanelModel();
        var firstGeneration = LoadTestViewerInNewContext();
        var secondGeneration = LoadTestViewerInNewContext();
        Assert.NotSame(firstGeneration, secondGeneration);

        AddViewer(panelModel, firstGeneration);
        AddViewer(panelModel, secondGeneration);

        Assert.Equal(new[] { firstGeneration.FullName }, panelModel.ListViewers());
        Assert.Same(secondGeneration, panelModel.CreateViewer(null, firstGeneration.FullName).GetType());
    }

    [Fact]
    public void RemoveViewer_RemovesViewerFromPreviousAssemblyGeneration()
    {
        var panelModel = CreatePanelModel();
        var firstGeneration = LoadTestViewerInNewContext();
        var secondGeneration = LoadTestViewerInNewContext();

        AddViewer(panelModel, firstGeneration);
        RemoveViewer(panelModel, secondGeneration);

        Assert.Empty(panelModel.ListViewers());
    }

    private static IPanelModel CreatePanelModel()
    {
        var type = typeof(TypeDInit).Assembly.GetType("TypeD.Models.PanelModel", throwOnError: true);
        return (IPanelModel)Activator.CreateInstance(type);
    }

    // Loads the test assembly into its own context, the same way a reloaded module gets a new assembly
    // instance; TypeD itself resolves through the default context, so IViewer stays the same type
    private static Type LoadTestViewerInNewContext()
    {
        var context = new AssemblyLoadContext("generation-" + Guid.NewGuid().ToString("N"));
        var assembly = context.LoadFromAssemblyPath(typeof(PanelModelTests).Assembly.Location);
        return assembly.GetType(typeof(TestViewer).FullName, throwOnError: true);
    }

    private static void AddViewer(IPanelModel panelModel, Type viewerType) =>
        typeof(IPanelModel).GetMethod(nameof(IPanelModel.AddViewer)).MakeGenericMethod(viewerType).Invoke(panelModel, null);

    private static void RemoveViewer(IPanelModel panelModel, Type viewerType) =>
        typeof(IPanelModel).GetMethod(nameof(IPanelModel.RemoveViewer)).MakeGenericMethod(viewerType).Invoke(panelModel, null);
}
