using Xunit;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="RowsIteratorUtils" />.
/// </summary>
public class RowIteratorUtilsTests
{
    [Fact]
    public void DetermineTypeByValues_BooleanValues_ReturnsBoolean()
    {
        // Arrange.
        var values = new[] { "F", "1", "0", "true", "OFF" };

        // Act.
        var type = RowsIteratorUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.Boolean, type);
    }

    [Fact]
    public void DetermineTypeByValues_DateTimeValues_ReturnsTimestamp()
    {
        // Arrange.
        var values = new[] { "2022-01-01", "2000-02-02" };

        // Act.
        var type = RowsIteratorUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.Timestamp, type);
    }

    [Fact]
    public void DetermineTypeByValues_IntegersWithNullsValues_ReturnsInteger()
    {
        // Arrange.
        var values = new[] { "12", "23", "", "13" };

        // Act.
        var type = RowsIteratorUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.Integer, type);
    }

    [Fact]
    public void DetermineTypeByValues_IntegersWithBreakValues_ReturnsString()
    {
        // Arrange.
        var values = new[] { "12", "23", "", "13", "11r" };

        // Act.
        var type = RowsIteratorUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.String, type);
    }

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
