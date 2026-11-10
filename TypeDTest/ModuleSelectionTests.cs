using TypeD.Models.Data;

namespace TypeDTest;

public class ModuleSelectionTests
{
    private static ModuleTypeO TypeO(params string[] dependencies)
    {
        var typeO = (ModuleTypeO)Activator.CreateInstance(typeof(ModuleTypeO), nonPublic: true);
        typeO.Dependencies.AddRange(dependencies);
        return typeO;
    }

    [Theory]
    [InlineData("0.1.0", "0.2.0", -1)]
    [InlineData("0.2.0", "0.2.0", 0)]
    [InlineData("0.10.0", "0.9.0", 1)]
    [InlineData("0.1.0.dev", "0.1.0", 0)]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("", "0.1.0", -1)]
    public void CompareVersionsComparesNumericParts(string a, string b, int expected)
    {
        Assert.Equal(expected, Math.Sign(ModuleSelection.CompareVersions(a, b)));
    }

    [Fact]
    public void LatestVersionIgnoresBranchTags()
    {
        Assert.Equal("0.10.0", ModuleSelection.LatestVersion(new[] { "0.9.0", "0.11.0.feature", "0.10.0", "0.1.0" }));
        Assert.Null(ModuleSelection.LatestVersion(new[] { "0.11.0.feature" }));
    }

    [Fact]
    public void ParseReadsAuthorNameVersionAndTags()
    {
        var dependency = ModuleDependency.Parse("Udrian@TypeOCore-0.1.0.dev");
        Assert.Equal(("Udrian", "TypeOCore", "0.1.0.dev", false, false), (dependency.Author, dependency.Name, dependency.Version, dependency.Nuget, dependency.IsLocal));

        var nuget = ModuleDependency.Parse("OpenTK-4.9.4;nuget");
        Assert.True(nuget.Nuget);
        Assert.Empty(nuget.Tags);

        var dev = ModuleDependency.Parse("Udrian@TypeD-0.1.0;dev");
        Assert.Equal(new[] { "dev" }, dev.Tags);

        var local = ModuleDependency.Parse("Udrian@Loc-C:/x/my-module;local");
        Assert.True(local.IsLocal);
        Assert.Equal("C:/x/my-module", local.LocalPath);
    }

    [Fact]
    public void ModuleDependenciesSkipNugetsTestOnlyAndTypeD()
    {
        var typeO = TypeO(
            "Udrian@TypeOCore-0.1.0",
            "Udrian@TypeOTK-0.2.0;dev",
            "Udrian@TypeD-0.1.0;dev",
            "OpenTK-4.9.4;nuget",
            "xunit-2.9.3;nuget;test",
            "Udrian@Mock-0.1.0;test");

        Assert.Equal(new[] { "TypeOCore", "TypeOTK" }, ModuleSelection.ModuleDependencies(typeO).Select(d => d.Name));
    }

    [Fact]
    public void RequiredVersionsKeepTheHighestRequirement()
    {
        var selected = new[]
        {
            ("A", TypeO("Udrian@Core-0.1.0")),
            ("B", TypeO("Udrian@Core-0.3.0", "Udrian@TK-0.2.0"))
        };

        var required = ModuleSelection.RequiredVersions(selected);

        Assert.Equal(("0.3.0", "B"), (required["Core"].Version, required["Core"].RequiredBy));
        Assert.Equal(("0.2.0", "B", "Udrian"), (required["TK"].Version, required["TK"].RequiredBy, required["TK"].Author));
    }

    [Fact]
    public void WarningWhenSelectedVersionIsLowerThanRequired()
    {
        var selected = new[] { ("A", TypeO("Udrian@Core-0.3.0")) };
        var required = ModuleSelection.RequiredVersions(selected);

        var warning = ModuleSelection.Warning("Core", "0.1.0", TypeO(), required, _ => null);

        Assert.Contains("updated to 0.3.0", warning);
        Assert.Contains("required by A", warning);
        Assert.Null(ModuleSelection.Warning("Core", "0.3.0", TypeO(), required, _ => null));
    }

    [Fact]
    public void WarningWhenModuleRequiresALowerVersionThanSelected()
    {
        var typeO = TypeO("Udrian@Core-0.1.0");
        var required = ModuleSelection.RequiredVersions(new[] { ("A", typeO) });

        var warning = ModuleSelection.Warning("A", "0.2.0", typeO, required, name => name == "Core" ? "0.3.0" : null);

        Assert.Contains("A may need an update", warning);
        Assert.Null(ModuleSelection.Warning("A", "0.2.0", typeO, required, name => name == "Core" ? "0.1.0" : null));
    }
}
