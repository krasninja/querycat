using Xunit;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Parser;

namespace QueryCat.UnitTests.Ast;

/// <summary>
/// Tests for <see cref="AstBuilder" />.
/// </summary>
public class AstBuilderTests
{
    [Fact]
    public async Task BuildFromString_QueryWithBetweenClause_ShouldAddBrackets()
    {
        // Arrange.
        var queryAstVisitor = new QueryAstVisitor();
        var astBuilder = new AstBuilder();

        // Act.
        var node = astBuilder.BuildProgramFromString(
            "SELECT * FROM read_file('') WHERE id BETWEEN 10 + 20 AND 30 AND id BETWEEN 40 AND 50;");

        // Assert.
        await queryAstVisitor.RunAsync(node, CancellationToken.None);
        var newQuery = queryAstVisitor.Get(node);
        Assert.Equal("SELECT * FROM read_file('') WHERE ((id) BETWEEN ((10) + (20)) AND (30)) AND ((id) BETWEEN (40) AND (50));",
            newQuery.Trim());
    }
}
