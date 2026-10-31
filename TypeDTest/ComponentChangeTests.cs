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

    [Fact]
    public void HasChanges_ReturnsFalseWhenPropertyMatchesItsDefinition()
    {
        var component = CreateComponent("default");
        component.OveriddenProperties[0].Value = "default";

        Assert.False(component.HasPropertyOverrides);
        Assert.False(component.HasChanges);
    }

    [Fact]
    public void HasChanges_ReturnsTrueWhenPropertyHasNoDefinitionValue()
    {
        var component = new Component();
        component.Properties.Add(new Property { Name = "AddedProperty", Value = "value" });

        Assert.True(component.HasPropertyOverridesFor("AddedProperty"));
        Assert.True(component.HasChanges);
    }

    [Fact]
    public void HasChanges_ReturnsTrueWhenInheritedChildWasRemoved()
    {
        var component = new Component();
        var child = CreateComponent("default");
        child.Removed = true;
        component.Children.Add(child);
        component.DefinitionChildren.Add(child);

        Assert.True(component.HasChanges);
    }

    [Fact]
    public void ChildOrderChanged_DetectsOnlyNonDefaultOrdering()
    {
        var first = new Component();
        var second = new Component();
        var added = new Component();
        var component = new Component();
        component.DefinitionChildren.AddRange([first, second]);
        component.Children.AddRange([first, second, added]);

        Assert.False(component.ChildOrderChanged);

        component.Children.Reverse();

        Assert.True(component.ChildOrderChanged);
    }

    [Fact]
    public void ComponentProperties_ResolveNamesInheritanceAndIcons()
    {
        var baseComponent = new Component
        {
            ClassName = nameof(Component),
            Namespace = typeof(Component).Namespace,
            Icon = "base.png"
        };
        var component = new Component
        {
            ClassName = "Child",
            Namespace = "Example",
            BaseInheritedComponent = baseComponent
        };

        Assert.Equal("Example.Child", component.FullName);
        Assert.Equal("Child", component.Name);
        Assert.Equal("base.png", component.ResolvedIcon);
        Assert.Same(baseComponent, component.RootBaseComponent);
        Assert.True(component.IsBasedOn(typeof(Component)));
        Assert.False(component.IsBasedOn(null));

        component.Properties.Add(new Property { Name = "Name", Value = "Friendly name" });
        component.Properties.Add(new Property { Name = "ID", Value = "component-id" });

        Assert.Equal("Friendly name", component.Name);
        component.Name = "Renamed";
        component.ID = "updated-id";
        Assert.Equal("Renamed", component.Properties[0].Value);
        Assert.Equal("updated-id", component.ID);
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
