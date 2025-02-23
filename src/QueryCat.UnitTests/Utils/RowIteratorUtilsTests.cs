using Xunit;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="RowsIteratorUtils" />.
/// </summary>
public class RowIteratorUtilsTests
{
    [Fact]
    public async Task DetermineIfHasHeader_RowsFrameWithHeader_ShouldReturnTrue()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("Name", DataType.String),
            new Column("Balance", DataType.String));
        rowsFrame.AddRow("Name", "Balance");
        rowsFrame.AddRow("Marina", 40000);
        rowsFrame.AddRow("Elena", 45000);

        // Act.
        var hasHeader = await RowsIteratorUtils.DetermineIfHasHeaderAsync(rowsFrame.GetIterator());

        // Assert.
        Assert.True(hasHeader);
    }

    [Fact]
    public async Task DetermineIfHasHeader_RowsFrameWithoutHeader_ShouldReturnTrue()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("Num", DataType.String));
        rowsFrame.AddRow(1);
        rowsFrame.AddRow(2);
        rowsFrame.AddRow(3);

        // Act.
        var hasHeader = await RowsIteratorUtils.DetermineIfHasHeaderAsync(rowsFrame.GetIterator());

        // Assert.
        Assert.False(hasHeader);
    }
}
