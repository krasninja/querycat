using Xunit;
using QueryCat.Backend.Types;

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
        var hour1 = DataTypeUtils.ParseInterval("1h");
        var hour2 = DataTypeUtils.ParseInterval("2 hours");
        var min1 = DataTypeUtils.ParseInterval("00:01:00");
        var min1Sec1Hour1 = DataTypeUtils.ParseInterval(" 2 min 1 second  1h   2s ");

        // Assert.
        Assert.Equal(TimeSpan.FromHours(1), hour1);
        Assert.Equal(TimeSpan.FromHours(2), hour2);
        Assert.Equal(TimeSpan.FromMinutes(1), min1);
        Assert.Equal(new TimeSpan(1, 2, 3), min1Sec1Hour1);
    }
}
