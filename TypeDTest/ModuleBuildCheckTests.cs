using System.Reflection;
using TypeD;
using TypeD.Models.Interfaces;
using Module = TypeD.Models.Data.Module;

namespace TypeDTest;

public class ModuleBuildCheckTests
{
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
}
