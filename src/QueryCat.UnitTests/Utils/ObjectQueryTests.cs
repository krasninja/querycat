using Xunit;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="ObjectQuery" />.
/// </summary>
public class ObjectQueryTests
{
    private class User
    {
        public int Id { get; set; }

        public Address Address { get; set; }
    }

    private class Address
    {
        public string City { get; set; }

        public string Street { get; set; }
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
        var value = ObjectQuery.Query(user, "Id");

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
        var value = ObjectQuery.Query(user, "this.Address.City");

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
        var value = ObjectQuery.Query(user, "this.Address.City");

        // Assert.
        Assert.Null(value);
    }
}
