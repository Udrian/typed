using Avalonia.Controls;
using TypeD.Models.Data;
using TypeD.ViewModel;

namespace TypeDTest;

public class PropertyMetadataTests
{
    [Fact]
    public void ComponentToolTip_ShowsCurrentBaseAndAccessibleCustomProperties()
    {
        var root = new Component { Namespace = "Engine", ClassName = "Entity" };
        var oldBase = new Component { Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = root };
        oldBase.CustomProperties.Add(new CustomProperty { Name = "Outdated" });
        var currentBase = new Component { Namespace = "Game", ClassName = "Actor", BaseInheritedComponent = root };
        currentBase.CustomProperties.Add(new CustomProperty
        {
            Name = "Speed", TypeName = typeof(int).FullName, Group = "Movement",
            Description = "Movement speed", Access = CustomPropertyAccess.Protected
        });
        currentBase.CustomProperties.Add(new CustomProperty { Name = "BaseSecret", Access = CustomPropertyAccess.Private });
        root.CustomProperties.Add(new CustomProperty { Name = "RootProperty", TypeName = typeof(bool).FullName });
        var component = new Component { Namespace = "Game", ClassName = "Player", BaseInheritedComponent = oldBase };
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Health", TypeName = typeof(int).FullName, Group = "Stats", Access = CustomPropertyAccess.Private
        });

        var tooltip = Assert.IsType<ScrollViewer>(component.CreateToolTip(parent => parent == oldBase ? currentBase : parent));
        var panel = Assert.IsType<StackPanel>(tooltip.Content);
        var metadata = Assert.IsType<Grid>(panel.Children[0]).Children.OfType<TextBlock>().Select(block => block.Text).ToList();
        Assert.Equal(new[] { "Component", "Game.Player", "Base type", "Game.Actor", "Root type", "Engine.Entity" }, metadata);
        Assert.Equal("Custom properties", Assert.IsType<TextBlock>(panel.Children[1]).Text);
        var properties = Assert.IsType<Grid>(panel.Children[2]).Children.OfType<TextBlock>().Select(block => block.Text).ToList();
        Assert.Contains("Health", properties);
        Assert.Contains("private System.Int32 - Stats", properties);
        Assert.Contains("Speed", properties);
        Assert.Contains("protected System.Int32 - Movement\nInherited from Game.Actor\nMovement speed", properties);
        Assert.Contains("RootProperty", properties);
        Assert.DoesNotContain("BaseSecret", properties);
        Assert.DoesNotContain("Outdated", properties);
    }

    [Fact]
    public void ComponentToolTip_HandlesComponentsWithoutBaseOrCustomProperties()
    {
        var component = new Component { Namespace = "Game", ClassName = "Root" };
        var panel = Assert.IsType<StackPanel>(Assert.IsType<ScrollViewer>(component.CreateToolTip()).Content);
        var metadata = Assert.IsType<Grid>(panel.Children[0]).Children.OfType<TextBlock>().Select(block => block.Text).ToList();
        Assert.Equal(new[] { "Component", "Game.Root", "Base type", "None", "Root type", "Game.Root" }, metadata);
        var properties = Assert.IsType<Grid>(panel.Children[2]).Children.OfType<TextBlock>().Select(block => block.Text);
        Assert.Contains("None", properties);
    }

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
