using Xunit;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="ProjectedRowsIterator" />.
/// </summary>
public sealed class ProjectedRowsIteratorTests
{
    [Fact]
    public void Projection_SourceRowsSet_CorrectTargetRowsSetWithColumns()
    {
        // Arrange.
        var table = new RowsFrame(
            new Column("Id", DataType.Integer),
            new Column("Name", DataType.String));
        table.AddRow(10, "Fedor");
        table.AddRow(20, "Igor");

        // Act.
        var tableIterator = table.GetIterator();
        var projectedIterator = new ProjectedRowsIterator(tableIterator);
        projectedIterator.AddFuncColumn(table.Columns[1],
            new FuncUnitRowsIteratorColumn(tableIterator, 1));
        var frame = projectedIterator.ToFrame();
        var firstRow = frame.First();

        // Assert.
        Assert.Equal("Fedor", firstRow[0].AsString);
    }
}
