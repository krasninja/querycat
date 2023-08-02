using Xunit;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="DistinctRowsIteratorIterator" />.
/// </summary>
public class DistinctRowsIteratorTests
{
    [Fact]
    public void Distinct_RowsSetWithDuplicates_ShouldReturnUnique()
    {
        // Arrange.
        var table = new RowsFrame(
            new Column("Id", DataType.Integer),
            new Column("Name", DataType.String));
        table.AddRow(10, "Anna");
        table.AddRow(20, "Marina M");
        table.AddRow(20, "Marina M");
        table.AddRow(30, "Marina M");

        // Act.
        var resultRowsSet = new DistinctRowsIteratorIterator(table.GetIterator()).ToFrame();

        // Assert.
        Assert.Equal(3, resultRowsSet.TotalRows);
    }
}
