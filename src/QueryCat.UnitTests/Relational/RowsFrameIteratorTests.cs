using Xunit;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="RowsFrameIterator" />.
/// </summary>
public sealed class RowsFrameIteratorTests
{
    private readonly RowsFrame _rowsFrame = new(
        new Column("name", DataType.String)
    );

    [Fact]
    public void MoveNext_RemoveRows_ShouldSkipRemoved()
    {
        // Arrange.
        _rowsFrame.AddRow("Zombie");
        _rowsFrame.AddRow("Witch");
        _rowsFrame.AddRow("Spider");
        var iterator = new RowsFrameIterator(_rowsFrame);
        var count = 0;

        // Act.
        _rowsFrame.RemoveRow(0);
        _rowsFrame.RemoveRow(2);
        while (iterator.MoveNext())
        {
            count++;
        }

        // Assert.
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData(2, CursorSeekOrigin.Begin, 6)]
    [InlineData(1, CursorSeekOrigin.Begin, 4)]
    [InlineData(2, CursorSeekOrigin.End, 2)]
    [InlineData(0, CursorSeekOrigin.End, 6)]
    [InlineData(0, CursorSeekOrigin.Current, 2)]
    public void Seek_RowsFrameWithRemovedRows_ShouldSeekCorrectly(int offset, CursorSeekOrigin seekOrigin, int expectedPosition)
    {
        // Arrange.
        _rowsFrame.AddRow("Mese Monster"); // 0-
        _rowsFrame.AddRow("Squid"); // 1-
        _rowsFrame.AddRow("Rover"); // 2 (0)
        _rowsFrame.AddRow("Sand Monster"); // 3-
        _rowsFrame.AddRow("Stray"); // 4 (1)
        _rowsFrame.AddRow("Piglin"); // 5-
        _rowsFrame.AddRow("Spider"); // 6 (2)
        var iterator = new RowsFrameIterator(_rowsFrame);

        // Act.
        _rowsFrame.RemoveRow(0);
        _rowsFrame.RemoveRow(1);
        _rowsFrame.RemoveRow(3);
        _rowsFrame.RemoveRow(5);
        iterator.Seek(offset, seekOrigin);

        // Assert.
        Assert.Equal(expectedPosition, iterator.Position);
    }
}
