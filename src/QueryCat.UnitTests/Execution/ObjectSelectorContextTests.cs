using Xunit;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="ObjectSelectorContext" />.
/// </summary>
public class ObjectSelectorContextTests
{
    private readonly ObjectSelectorContext _selectorContext = new();

    private class User
    {
        public required string Name { get; init; }

        public int Age { get; init; }

        public Address? Address { get; init; }
    }

    private class Address
    {
        public required string City { get; init; }
    }

    private class BaseItem
    {
        public virtual string Title { get; set; } = "base";

        public string Code => "base";
    }

    private class DerivedItem : BaseItem
    {
        public override string Title { get; set; } = "derived";

        public new string Code { get; set; } = "derived";
    }

    [Fact]
    public void TokenFrom_SimpleObjectQuery_ReturnsExpectedResult()
    {
        // Arrange.
        var user = new User
        {
            Name = "John Doe",
        };

        // Act.
        var token = ObjectSelectorContext.Token.From(user, u => u.Name);

        // Assert.
        Assert.Equal("John Doe", token!.Value.Value!.ToString());
    }

    [Fact]
    public void TokenFrom_ComplexObjectQuery_ReturnsExpectedResult()
    {
        // Arrange.
        var user = new User
        {
            Name = "John Doe",
        };

        // Act.
        var token1 = ObjectSelectorContext.Token.From(user, u => u.Name.Length);
        var token2 = ObjectSelectorContext.Token.From(user, u => u.Name[0]);

        // Assert.
        Assert.Equal("John Doe".Length.ToString(), token1!.Value.Value!.ToString());
        Assert.Equal("J", token2!.Value.Value!.ToString());
    }

    [Fact]
    public void TokenFrom_OverriddenProperty_ShouldReturnOwnerProperty()
    {
        // Arrange.
        var item = new DerivedItem();

        // Act.
        var token = ObjectSelectorContext.Token.From(item, i => i.Title);

        // Assert.
        Assert.Equal("derived", token!.Value.Value);
        Assert.Equal(typeof(DerivedItem), token.Value.PropertyInfo!.DeclaringType);
    }

    [Fact]
    public void TokenFrom_HiddenPropertyViaBaseReference_ShouldReturnOwnerValue()
    {
        // Arrange.
        BaseItem item = new DerivedItem();

        // Act.
        var token = ObjectSelectorContext.Token.From(item, i => i.Code);

        // Assert.
        Assert.Equal("derived", token!.Value.Value);
        Assert.Equal(typeof(DerivedItem), token.Value.PropertyInfo!.DeclaringType);
        Assert.True(token.Value.PropertyInfo.CanWrite);
    }

    [Fact]
    public void TokenFrom_ValueTypeProperty_ShouldSetPropertyInfo()
    {
        // Arrange.
        var user = new User
        {
            Name = "John Doe",
            Age = 30,
        };

        // Act.
        var token = ObjectSelectorContext.Token.From(user, u => u.Age);

        // Assert.
        Assert.Equal(30, token!.Value.Value);
        Assert.Equal(nameof(User.Age), token.Value.PropertyInfo!.Name);
    }

    [Fact]
    public void TokenFrom_NestedProperty_ShouldReturnValue()
    {
        // Arrange.
        var user = new User
        {
            Name = "John Doe",
            Address = new Address
            {
                City = "Moscow",
            },
        };

        // Act.
        var token = ObjectSelectorContext.Token.From(user, u => u.Address!.City);

        // Assert.
        Assert.Equal("Moscow", token!.Value.Value);
        Assert.Null(token.Value.PropertyInfo);
    }
}
