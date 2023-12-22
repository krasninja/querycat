using Xunit;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="GraphQLQueryBuilder" />.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GraphQLQueryBuilderTests
{
    [Fact]
    public void Build_SimpleQueryWithFieldsAndParams_CorrectQuery()
    {
        // Arrange.
        var builder = new GraphQLQueryBuilder("user")
            .AddParam("id", 10)
            .AddParam("amount", 345.4)
            .AddField("firstName")
            .AddField("lastName")
            .AddField("address", b =>
            {
                b
                    .AddParam("type", "main")
                    .AddField("city");
            });

        // Act.
        var output = builder.Build().ToString();

        // Assert.
        Assert.Equal(
            @"
query {
  user(id:10, amount:345.4) {
    firstName
    lastName
    address(type:""main"") {
      city
    }
  }
}".Trim(), output.Trim());
    }
}
