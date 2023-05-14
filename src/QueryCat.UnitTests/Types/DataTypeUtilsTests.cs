using Xunit;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Types;

/// <summary>
/// Tests for <see cref="DataTypeUtils" />.
/// </summary>
public class DataTypeUtilsTests
{
    [Fact]
    public void ParseInterval_VariousValues_ShouldParse()
    {
        // Act.
        var hour1 = IntervalParser.ParseInterval("1h");
        var hour2 = IntervalParser.ParseInterval("2 hours");
        var min1 = IntervalParser.ParseInterval("00:01:00");
        var min1Sec1Hour1 = IntervalParser.ParseInterval(" 2 min 1 second  1h   2s ");

        // Assert.
        Assert.Equal(TimeSpan.FromHours(1), hour1);
        Assert.Equal(TimeSpan.FromHours(2), hour2);
        Assert.Equal(TimeSpan.FromMinutes(1), min1);
        Assert.Equal(new TimeSpan(1, 2, 3), min1Sec1Hour1);
    }

    [Fact]
    public void SerializeDeserialize_VariousValues_ShouldMatch()
    {
        // Arrange.
        var valuesArray = new[]
        {
            new VariantValue(10),
            new VariantValue(true),
            new VariantValue("test string"),
            new VariantValue(new DateTime(2022, 1, 1)),
            new VariantValue(new TimeSpan(10, 10, 10)),
            VariantValue.Null
        };

        // Act.
        var serializedArray = valuesArray.Select(DataTypeUtils.SerializeVariantValue).ToArray();
        var deserializedArray = serializedArray.Select(v => DataTypeUtils.DeserializeVariantValue(v)).ToArray();

        // Assert.
        for (var i = 0; i < valuesArray.Length; i++)
        {
            Assert.Equal(valuesArray[i], deserializedArray[i]);
        }
    }
}
