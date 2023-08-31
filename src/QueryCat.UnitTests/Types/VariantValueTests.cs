using Xunit;
using QueryCat.Backend.Core.Types;

namespace QueryCat.UnitTests.Types;

/// <summary>
/// Tests for <see cref="VariantValue" />.
/// </summary>
public sealed class VariantValueTests
{
    [Fact]
    public void Create_IntType_ShouldKeepValue()
    {
        // Arrange.
        var intValue = new VariantValue(42);

        // Assert.
        Assert.Equal(42, intValue.AsInteger);
        Assert.Equal(new VariantValue(42), intValue);
    }

    [Fact]
    public void Create_DateTimeType_ShouldKeepValue()
    {
        // Arrange.
        var datetimeValue = new VariantValue(new DateTime(2022, 3, 1));

        // Assert.
        Assert.Equal(new DateTime(2022, 3, 1), datetimeValue.AsTimestamp);
        Assert.NotEqual(datetimeValue, new VariantValue(42));
    }

    [Fact]
    public void Convert_FromTypeToMatchType_ShouldConvert()
    {
        VariantValue Cast(VariantValue value, DataType targetType)
            => value.Cast(targetType);

        // Assert.
        Assert.Equal(new VariantValue(1), Cast(new VariantValue("1"), DataType.Integer));
        Assert.Equal(new VariantValue(true), Cast(new VariantValue("1"), DataType.Boolean));
        Assert.Equal(new VariantValue(false), Cast(new VariantValue("0"), DataType.Boolean));
        Assert.Equal(new VariantValue(new DateTime(2022, 1, 1)),
            Cast(new VariantValue("2022-01-01"), DataType.Timestamp));
        Assert.Equal(new VariantValue(10), Cast(new VariantValue(10.3), DataType.Integer));

        Assert.True(Cast(VariantValue.Null, DataType.Integer).IsNull);
    }
}
