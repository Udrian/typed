using TypeD.Code;
using TypeD.Models.Data;

namespace TypeDTest;

public class CustomPropertyTests
{
    private class TestComponentTypeCode : ComponentTypeCode
    {
        public TestComponentTypeCode(Component component) : base(component) { }

        public override Type TypeOBaseType => typeof(object);

        protected override void InitClass() { }

        protected override void InitTypeDClass() { }

        public string TypeDOutput => TypeDFile.Output.ToString();
    }

    private static Component CreateComponent()
    {
        return new Component
        {
            ClassName = "Player",
            Namespace = "Example.Entities"
        };
    }

    [Fact]
    public void Generate_WritesCustomPropertiesWithTypeOPropertyAttribute()
    {
        var component = CreateComponent();
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Speed",
            TypeName = "System.Double",
            DefaultValue = 5.0,
            Group = "Stats",
            Access = CustomPropertyAccess.Public
        });
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Lives",
            TypeName = "System.Int32",
            DefaultValue = 3,
            Access = CustomPropertyAccess.Private
        });
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Title",
            TypeName = "System.String",
            DefaultValue = "Hello",
            Group = "Info",
            Access = CustomPropertyAccess.Protected
        });
        var code = new TestComponentTypeCode(component);

        code.Generate();
        var output = code.TypeDOutput;

        Assert.Contains("[TypeOEngine.Typedeaf.Core.Attributes.TypeOProperty(Group = \"Stats\")]", output);
        Assert.Contains("public double Speed { get; set; } = 5;", output);
        Assert.Contains("[TypeOEngine.Typedeaf.Core.Attributes.TypeOProperty(Group = \"General\")]", output);
        Assert.Contains("private int Lives { get; set; } = 3;", output);
        Assert.Contains("[TypeOEngine.Typedeaf.Core.Attributes.TypeOProperty(Group = \"Info\")]", output);
        Assert.Contains("protected string Title { get; set; } = \"Hello\";", output);
    }

    [Fact]
    public void Generate_SkipsInitializerForNullDefaultAndUnresolvedType()
    {
        var component = CreateComponent();
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Target",
            TypeName = "System.String",
            DefaultValue = null,
            Access = CustomPropertyAccess.Public
        });
        var code = new TestComponentTypeCode(component);

        code.Generate();

        Assert.Contains("public string Target { get; set; }", code.TypeDOutput);
        Assert.DoesNotContain("Target { get; set; } =", code.TypeDOutput);
    }

    [Fact]
    public void Generate_EscapesQuotesInGroupName()
    {
        var component = CreateComponent();
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Notes",
            TypeName = "System.String",
            DefaultValue = "",
            Group = "My \"Special\" Group",
            Access = CustomPropertyAccess.Public
        });
        var code = new TestComponentTypeCode(component);

        code.Generate();

        Assert.Contains("[TypeOEngine.Typedeaf.Core.Attributes.TypeOProperty(Group = \"My \\\"Special\\\" Group\")]", code.TypeDOutput);
    }

    [Fact]
    public void Generate_WritesEscapedDescriptionArgument()
    {
        var component = CreateComponent();
        component.CustomProperties.Add(new CustomProperty
        {
            Name = "Health",
            Description = "How much \"damage\"\nit can take",
            TypeName = "System.Int32",
            DefaultValue = 10,
            Group = "Stats",
            Access = CustomPropertyAccess.Public
        });
        var code = new TestComponentTypeCode(component);

        code.Generate();

        Assert.Contains("[TypeOEngine.Typedeaf.Core.Attributes.TypeOProperty(\"How much \\\"damage\\\"\\nit can take\", Group = \"Stats\")]", code.TypeDOutput);
        Assert.Contains("public int Health { get; set; } = 10;", code.TypeDOutput);
    }

    [Fact]
    public void PropertyGenerate_WritesAttributesAndInitializer()
    {
        var writer = new Codalyzer.CodeWriter { TargetFile = new Codalyzer.CodeFile() };
        var property = new Codalyzer.Property("public int Count")
        {
            Attributes = new List<string> { "[SomeAttribute]" },
            Initializer = "42"
        };

        property.Generate(writer);
        var output = writer.TargetFile.Output.ToString();

        Assert.Contains("[SomeAttribute]", output);
        Assert.Contains("public int Count { get; set; } = 42;", output);
    }

    [Fact]
    public void CustomProperty_NormalizesGroupAndCopies()
    {
        var property = new CustomProperty
        {
            Name = "Speed",
            TypeName = "System.Double",
            DefaultValue = 1.0,
            Group = "  ",
            Access = CustomPropertyAccess.Protected
        };

        Assert.Equal("General", property.Group);

        property.Group = " Stats ";
        property.Description = "Movement speed";
        var copy = new CustomProperty(property);

        Assert.Equal("Speed", copy.Name);
        Assert.Equal("Movement speed", copy.Description);
        Assert.Equal("System.Double", copy.TypeName);
        Assert.Equal(1.0, copy.DefaultValue);
        Assert.Equal("Stats", copy.Group);
        Assert.Equal(CustomPropertyAccess.Protected, copy.Access);
    }
}
