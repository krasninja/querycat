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
    public void BuildFromString_QueryWithBetweenClause_ShouldAddBrackets()
    {
        // Arrange.
        var astBuilder = new AstBuilder();
        var queryAstVisitor = new QueryAstVisitor();

        // Act.
        var node = astBuilder.BuildProgramFromString(
            "SELECT * FROM read_file('') WHERE id BETWEEN 10 + 20 AND 30 AND id BETWEEN 40 AND 50;");

        // Assert.
        queryAstVisitor.Run(node);
        var newQuery = QueryAstVisitor.GetString(node);
        Assert.Equal("SELECT * read_file('') WHERE ((id) BETWEEN ((10) + (20)) AND (30)) AND ((id) BETWEEN (40) AND (50))",
            newQuery);
    }
}
