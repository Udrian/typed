using TypeD.Models.Data;

public class ComponentChangeTests
{
    [Fact]
    public void HasChanges_ReturnsFalseForUnmodifiedDefinitionChild()
    {
        var parent = new Component();
        var child = CreateComponent("default");
        parent.Children.Add(child);
        parent.DefinitionChildren.Add(child);

        Assert.False(parent.HasChanges);
    }

    [Fact]
    public void HasChanges_ReturnsTrueForChangedDefinitionGrandchild()
    {
        var parent = new Component();
        var child = new Component();
        var grandchild = CreateComponent("changed");
        grandchild.OveriddenProperties[0].Value = "default";
        parent.Children.Add(child);
        parent.DefinitionChildren.Add(child);
        child.Children.Add(grandchild);
        child.DefinitionChildren.Add(grandchild);

        Assert.True(parent.HasChanges);
    }

    [Fact]
    public void HasChanges_ReturnsTrueForAddedChild()
    {
        var parent = new Component();
        parent.Children.Add(CreateComponent("default"));

        Assert.True(parent.HasChanges);
    }

    private static Component CreateComponent(string value)
    {
        var component = new Component();
        component.Properties.Add(new Property
        {
            Name = "PropertyA",
            Value = value
        });
        component.OveriddenProperties.Add(new Property
        {
            Name = "PropertyA",
            Value = value
        });
        return component;
    }
}
