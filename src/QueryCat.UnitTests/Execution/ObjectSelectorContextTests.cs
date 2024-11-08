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
}
