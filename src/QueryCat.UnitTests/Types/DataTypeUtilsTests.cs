using Xunit;
using QueryCat.Backend.Core.Types;

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

    [Fact]
    public void SerializeDeserialize_Array_ShouldMatch()
    {
        // Arrange.
        var arrayValue = VariantValue.CreateFromObject(new List<VariantValue>
        {
            new(25),
            new(true),
            new("test string"),
            new(new DateTime(2022, 1, 1)),
            VariantValue.Null
        });

        // Act.
        var serializedArray = DataTypeUtils.SerializeVariantValue(arrayValue);
        var deserializedArray = DataTypeUtils.DeserializeVariantValue(serializedArray).AsArrayUnsafe;

        // Assert.
        Assert.Equal(25, deserializedArray[0].AsInteger);
        Assert.True(deserializedArray[1].AsBoolean);
        Assert.Equal("test string", deserializedArray[2].AsString);
        Assert.Equal(new DateTime(2022, 1, 1), deserializedArray[3].AsTimestamp);
        Assert.True(deserializedArray[4].IsNull);
    }

    [Fact]
    public void SerializeDeserialize_Map_ShouldMatch()
    {
        // Arrange.
        var mapValue = VariantValue.CreateFromObject(new Dictionary<VariantValue, VariantValue>
        {
            [new(25)] = new(10),
            [new("test string")] = VariantValue.Null,
            [new(true)] = VariantValue.CreateFromObject(new List<VariantValue>
            {
                new(1),
                new(2),
                new("3"),
            }),
        });

        // Act.
        var serializedMap = DataTypeUtils.SerializeVariantValue(mapValue);
        var deserializedMap = DataTypeUtils.DeserializeVariantValue(serializedMap).AsMapUnsafe;

        // Assert.
        Assert.Equal(3, deserializedMap.Count);
        Assert.Equal(10, deserializedMap[new(25)].AsInteger);
        Assert.True(deserializedMap[new("test string")].IsNull);
        Assert.Equal(2, deserializedMap[new(true)].AsArrayUnsafe[1].AsInteger);
        Assert.Equal("3", deserializedMap[new(true)].AsArrayUnsafe[2].AsString);
    }
}
