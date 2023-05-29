using Xunit;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="DataTypeUtils" />.
/// </summary>
public class DataTypeTests
{
    [Fact]
    public void DetermineTypeByValues_BooleanValues_ReturnsBoolean()
    {
        // Arrange.
        var values = new[] { "F", "1", "0", "true", "OFF" };

        // Act.
        var type = DataTypeUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.Boolean, type);
    }

    [Fact]
    public void DetermineTypeByValues_DateTimeValues_ReturnsTimestamp()
    {
        // Arrange.
        var values = new[] { "2022-01-01", "2000-02-02" };

        // Act.
        var type = DataTypeUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.Timestamp, type);
    }

    [Fact]
    public void DetermineTypeByValues_IntegersWithNullsValues_ReturnsInteger()
    {
        // Arrange.
        var values = new[] { "12", "23", "", "13" };

        // Act.
        var type = DataTypeUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.Integer, type);
    }

    [Fact]
    public void DetermineTypeByValues_IntegersWithBreakValues_ReturnsString()
    {
        // Arrange.
        var values = new[] { "12", "23", "", "13", "11r" };

        // Act.
        var type = DataTypeUtils.DetermineTypeByValues(values);

        // Assert.
        Assert.Equal(DataType.String, type);
    }
}
