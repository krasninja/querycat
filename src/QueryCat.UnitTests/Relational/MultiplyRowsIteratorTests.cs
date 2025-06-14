using Xunit;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="MultiplyRowsIterator" />.
/// </summary>
public class MultiplyRowsIteratorTests
{
    [Fact]
    public async Task Multiply_SourceRowsSet_ShouldProduceRelationalAlgebraMultiply()
    {
        // Arrange.
        var table1 = new RowsFrame(
            new Column("Id", DataType.Integer),
            new Column("Name", DataType.String));
        table1.AddRow(1, "Marina");
        table1.AddRow(2, "Lena");
        var table2 = new RowsFrame(
            new Column("City", DataType.String));
        table2.AddRow("Borodino");
        table2.AddRow("Krasnoyarsk");
        table2.AddRow("Ekaterinburg");

        /*
         * 0: 1,Marina,Borodino
         * 1: 1,Marina,Krasnoyarsk
         * 2: 1,Marina,Ekaterinburg
         * 3: 2,Lena,Borodino
         * 4: 2,Lena,Krasnoyarsk
         * 5: 2,Lena,Ekaterinburg
         */

        // Act.
        var multiplyRowsIterator = new MultiplyRowsIterator(table1.GetIterator(), table2.GetIterator());
        var resultRowsFrame = new RowsFrame(multiplyRowsIterator.Columns);
        await multiplyRowsIterator.ToFrameAsync(resultRowsFrame);

        // Assert.
        Assert.Equal(6, resultRowsFrame.TotalRows);
        Assert.Equal(1, resultRowsFrame.GetRow(0)["Id"].AsInteger);
        Assert.Equal("Borodino", resultRowsFrame.GetRow(0)["City"].AsString);
        Assert.Equal("Krasnoyarsk", resultRowsFrame.GetRow(1)["City"].AsString);
        Assert.Equal(1, resultRowsFrame.GetRow(2)["Id"].AsInteger);
        Assert.Equal(2, resultRowsFrame.GetRow(4)["Id"].AsInteger);
        Assert.Equal("Ekaterinburg", resultRowsFrame.GetRow(5)["City"].AsString);
    }
}
