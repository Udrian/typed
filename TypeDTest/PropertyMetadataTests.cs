using TypeD.Models.Data;
using TypeD.ViewModel;

namespace TypeDTest;

public class PropertyMetadataTests
{
    [Theory]
    [InlineData("pos", true)]
    [InlineData("TRANSFORM", true)]
    [InlineData("  player  ", true)]
    [InlineData("missing", false)]
    [InlineData("", true)]
    [InlineData(null, true)]
    public void Filter_MatchesNameGroupAndValue(string filter, bool expected)
    {
        var node = new PropertyNode(new Component(), new Property
        {
            Name = "Position",
            Group = "Transform",
            Value = "Player"
        }, (_, _) => { });

        Assert.Equal(expected, node.MatchesFilter(filter));
    }

    [Fact]
    public void Filter_UsesCurrentValueAndHandlesNullAndNumericValues()
    {
        var property = new Property { Name = "Count", Value = 123 };
        var node = new PropertyNode(new Component(), property, (_, _) => { });

        Assert.True(node.MatchesFilter("123"));
        property.Value = 456;
        Assert.False(node.MatchesFilter("123"));
        Assert.True(node.MatchesFilter("456"));
        property.Value = null;
        Assert.False(node.MatchesFilter("456"));
        Assert.True(node.MatchesFilter("general"));
    }

    [Theory]
    [InlineData(null, "General")]
    [InlineData("", "General")]
    [InlineData("  ", "General")]
    [InlineData(" Transform ", "Transform")]
    public void Group_NormalizesEmptyAndPaddedNames(string value, string expected)
    {
        var property = new Property { Group = value };

        Assert.Equal(expected, property.Group);
        Assert.Equal(expected, new PropertyNode(new Component(), property, (_, _) => { }).Group);
    }

    [Fact]
    public void Copy_PreservesGroupAndDeclaringClass()
    {
        var original = new Property
        {
            Name = "Position",
            Group = "Transform",
            DeclaringTypeName = "Game.BaseEntity",
            Description = "World position"
        };

        var copy = new Property(original);

        Assert.Equal("Transform", copy.Group);
        Assert.Equal("Game.BaseEntity", copy.DeclaringTypeName);
        Assert.Equal(original.Description, copy.Description);
    }

    [Fact]
    public void NameToolTip_IncludesDescriptionDeclaringClassAndValidation()
    {
        var node = new PropertyNode(new Component(), new Property
        {
            Description = "World position",
            DeclaringTypeName = "Game.BaseEntity"
        }, (_, _) => { });
        var notifications = new List<string>();
        node.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        node.ValidationError = "Invalid position";

        Assert.Contains("World position", node.NameToolTip);
        Assert.Contains("Declared by: Game.BaseEntity", node.NameToolTip);
        Assert.Contains("Invalid position", node.NameToolTip);
        Assert.Contains(nameof(PropertyNode.NameToolTip), notifications);

        node.ValidationError = null;
        Assert.DoesNotContain("Invalid position", node.NameToolTip);
    }
}
