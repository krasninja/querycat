using Xunit;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="RowsIteratorUtils" />.
/// </summary>
public class RowsIteratorUtilsTests
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
        var hasHeader = await RowsIteratorUtils.DetermineIfHasHeaderAsync(rowsFrame.GetIterator(), cancellationToken: TestContext.Current.CancellationToken);

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
        var hasHeader = await RowsIteratorUtils.DetermineIfHasHeaderAsync(rowsFrame.GetIterator(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert.
        Assert.False(hasHeader);
    }

    [Fact]
    public async Task ResolveColumnsAttributes_TraceAndTimestampColumns_ShouldSetAttributes()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("request_id", DataType.String),
            new Column("occurred_at", DataType.Timestamp));

        // Act.
        await RowsIteratorUtils.ResolveColumnsAttributesAsync(rowsFrame.GetIterator(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert.
        Assert.True(rowsFrame.Columns[0].Attributes.ContainsKey(Column.AttributeTrace));
        Assert.True(rowsFrame.Columns[1].Attributes.ContainsKey(Column.AttributeTimestamp));
    }

    [Fact]
    public async Task ResolveColumnsAttributes_TwoIdColumns_ShouldSetIdAttributeOnFirstOnly()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("ID", DataType.Integer),
            new Column("identifier", DataType.String));

        // Act.
        await RowsIteratorUtils.ResolveColumnsAttributesAsync(rowsFrame.GetIterator(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert.
        Assert.True(rowsFrame.Columns[0].Attributes.ContainsKey(Column.AttributeId));
        Assert.False(rowsFrame.Columns[1].HasAttributes);
    }

    [Fact]
    public async Task ResolveColumnsAttributes_StringColumnNamedDate_ShouldNotSetTimestampAttribute()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("date", DataType.String),
            new Column("event_time", DataType.Timestamp));

        // Act.
        await RowsIteratorUtils.ResolveColumnsAttributesAsync(rowsFrame.GetIterator(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert.
        Assert.False(rowsFrame.Columns[0].HasAttributes);
        Assert.True(rowsFrame.Columns[1].Attributes.ContainsKey(Column.AttributeTimestamp));
    }

    [Fact]
    public async Task ResolveColumnsAttributes_NonMatchingColumns_ShouldNotSetAttributes()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(
            new Column("name", DataType.String),
            new Column("balance", DataType.Numeric),
            new Column("updated", DataType.Timestamp));

        // Act.
        await RowsIteratorUtils.ResolveColumnsAttributesAsync(rowsFrame.GetIterator(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert.
        Assert.False(rowsFrame.Columns[0].HasAttributes);
        Assert.False(rowsFrame.Columns[1].HasAttributes);
        Assert.False(rowsFrame.Columns[2].HasAttributes);
    }
}
