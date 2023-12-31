using Xunit;
using QueryCat.Backend.Functions;

namespace QueryCat.UnitTests.Functions;

/// <summary>
/// Tests for <see cref="ObjectFunctions" />.
/// </summary>
public class ObjectFunctionsTests
{
    private class User
    {
        public int Id { get; set; }

        public Address? Address { get; set; }
    }

    private class Address
    {
        public string City { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;
    }

    [Fact]
    public void Query_PropertyQuery_Evaluate()
    {
        // Arrange.
        var user = new User
        {
            Id = 10,
        };

        // Act.
        var value = ObjectFunctions.Query(user, "Id");

        // Assert.
        Assert.Equal(10, value);
    }

    [Fact]
    public void Query_PropertyQueryWithInner_Evaluate()
    {
        // Arrange.
        var user = new User
        {
            Id = 10,
            Address = new Address
            {
                City = "Borodino",
            },
        };

        // Act.
        var value = ObjectFunctions.Query(user, "this.Address.City");

        // Assert.
        Assert.Equal("Borodino", value);
    }

    [Fact]
    public void Query_PropertyQueryWithInnerAndNull_Evaluate()
    {
        // Arrange.
        var user = new User
        {
            Id = 10
        };

        // Act.
        var value = ObjectFunctions.Query(user, "this.Address.City");

        // Assert.
        Assert.Null(value);
    }
}
