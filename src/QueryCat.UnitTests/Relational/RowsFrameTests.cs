using Xunit;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="RowsFrame" />.
/// </summary>
public sealed class RowsFrameTests
{
    private readonly RowsFrame _rowsFrame = new(
        new RowsFrameOptions(),
        new Column("Id", DataType.Integer),
        new Column("Name", DataType.String));

    [Fact]
    public void WriteRow_InsertData_ShouldGetData()
    {
        // Act.
        _rowsFrame.AddRow(10, "Bob");
        _rowsFrame.AddRow(20, "Bobrovich");
        var row = _rowsFrame.First();

        // Assert.
        Assert.Equal(10, row[0].AsInteger);
        Assert.Equal("Bob", row[1].AsString);
    }

    [Fact]
    public void Next_EmptyData_ShouldReturnFalse()
    {
        // Act.
        _rowsFrame.AddRow(10, "Bob");
        using var iterator = _rowsFrame.GetEnumerator();
        iterator.MoveNext();

        // Assert.
        Assert.False(iterator.MoveNext());
    }
}
