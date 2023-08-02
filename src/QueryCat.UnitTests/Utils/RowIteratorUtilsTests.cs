using Xunit;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="RowsIteratorUtils" />.
/// </summary>
public class RowIteratorUtilsTests
{
    [Fact]
    public void DetermineIfHasHeader_RowsFrameWithHeader_ShouldReturnTrue()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("Name", DataType.String),
            new Column("Balance", DataType.String));
        rowsFrame.AddRow("Name", "Balance");
        rowsFrame.AddRow("Marina", 40000);
        rowsFrame.AddRow("Elena", 45000);

        // Act.
        var hasHeader = RowsIteratorUtils.DetermineIfHasHeader(rowsFrame.GetIterator());

        // Assert.
        Assert.True(hasHeader);
    }

    [Fact]
    public void DetermineIfHasHeader_RowsFrameWithoutHeader_ShouldReturnTrue()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("Num", DataType.String));
        rowsFrame.AddRow(1);
        rowsFrame.AddRow(2);
        rowsFrame.AddRow(3);

        // Act.
        var hasHeader = RowsIteratorUtils.DetermineIfHasHeader(rowsFrame.GetIterator());

        // Assert.
        Assert.False(hasHeader);
    }
}
