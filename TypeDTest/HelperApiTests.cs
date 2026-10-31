using System.Text.Json;
using TypeD.Helpers;
using TypeD.Models.Data;

public class HelperApiTests
{
    [Theory]
    [InlineData("My component-name", "Mycomponentname")]
    [InlineData("9lives", "_9lives")]
    [InlineData("class", "_class")]
    [InlineData("!!!", "_")]
    [InlineData(null, "_")]
    public void CSharpIdentifier_SanitizeReturnsValidIdentifier(string name, string expected)
    {
        Assert.Equal(expected, CSharpIdentifier.Sanitize(name));
    }

    [Fact]
    public void FilterHelper_ExcludesNonMatchesAndExplicitExcludesButIncludeWins()
    {
        var filter = new FilterHelper
        {
            Filters = "button; debug",
            Exclude = "debug",
            Include = "special"
        };

        Assert.False(filter.Filter("MainButton"));
        Assert.True(filter.Filter("Camera"));
        Assert.True(filter.Filter("debug"));
        Assert.False(filter.Filter("special"));
    }

    [Fact]
    public void FilterHelper_WithNoRulesDoesNotExcludeItems()
    {
        Assert.False(new FilterHelper().Filter("anything"));
    }

    [Fact]
    public void Flatten_ReturnsItemsDepthFirstIncludingNestedItems()
    {
        var flattened = new[] { 1, 4 }
            .Flatten(value => value switch
            {
                1 => new[] { 2, 3 },
                2 => new[] { 5 },
                _ => Array.Empty<int>()
            });

        Assert.Equal([1, 2, 5, 3, 4], flattened);
    }

    [Fact]
    public void PropertyCopyConstructor_CopiesPropertyValuesAndMetadata()
    {
        var original = new Property
        {
            Name = "Health",
            Description = "Current health",
            Value = 42,
            Type = typeof(int),
            ReadOnly = true,
            FromComponent = new Component()
        };

        var copy = new Property(original);

        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Description, copy.Description);
        Assert.Equal(42, copy.ToValue<int>());
        Assert.Equal(original.Type, copy.Type);
        Assert.True(copy.ReadOnly);
        Assert.Same(original.FromComponent, copy.FromComponent);
    }

    [Fact]
    public void EditorShortcut_FromKeyIncludesModifiersInStableOrder()
    {
        Assert.Equal("Ctrl+Alt+Shift+Meta+K", EditorShortcut.FromKey("K", true, true, true, true));
        Assert.Equal("Enter", EditorShortcut.FromKey("Enter", false, false, false, false));
    }

    [Fact]
    public void Json_SerializeAndDeserializeRoundTripsData()
    {
        var directory = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(directory, "sample.json");
            var original = new JsonTestData { Name = "scene", Count = 3 };

            Assert.True(JSON.Serialize(original, filePath));
            var deserialized = JSON.Deserialize<JsonTestData>(filePath);

            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.Count, deserialized.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Json_OptionsSupportWebNamingAndCaseInsensitiveReads()
    {
        var data = new JsonTestData { Name = "scene", Count = 3 };
        var json = JsonSerializer.Serialize(data, JSON.CreateOptions(web: true));
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "lowercase.json");

        try
        {
            File.WriteAllText(filePath, "{\"name\":\"scene\",\"count\":3}");

            Assert.Contains("\"name\":\"scene\"", json);
            Assert.Equal("scene", JSON.Deserialize<JsonTestData>(filePath, ignoreCase: true).Name);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Json_DeserializeMissingFileReturnsDefault()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        Assert.Null(JSON.Deserialize<JsonTestData>(missingPath));
    }

    [Fact]
    public void FileHelper_FetchStringListReadsDelimitedBlock()
    {
        var directory = CreateTempDirectory();
        try
        {
            var filePath = Path.Combine(directory, "items.txt");
            File.WriteAllLines(filePath, ["<items>", "<First>", "<Second>", "</items>", "<Ignored>"]);

            Assert.Equal(["First", "Second"], FileHelper.FetchStringList(filePath, "<items>", "</items>"));
            Assert.Empty(FileHelper.FetchStringList(Path.Combine(directory, "missing.txt"), "<items>", "</items>"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(null, null, null, "Warning CODE123: Be careful")]
    [InlineData("file.cs", 12, 4, "file.cs(12,4): Error CODE123: Be careful")]
    [InlineData("file.cs", 12, null, "file.cs(12): Error CODE123: Be careful")]
    public void BuildIssue_ToStringFormatsOptionalLocationAndCode(
        string file,
        int? line,
        int? column,
        string expected)
    {
        var issue = new BuildIssue
        {
            Severity = file == null ? "Warning" : "Error",
            Code = "CODE123",
            Message = "Be careful",
            File = file,
            Line = line,
            Column = column
        };

        Assert.Equal(expected, issue.ToString());
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TypeDTest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class JsonTestData
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
