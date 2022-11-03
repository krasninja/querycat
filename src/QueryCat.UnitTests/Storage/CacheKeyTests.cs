using Xunit;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Storage;

/// <summary>
/// Tests for <see cref="CacheKey" />.
/// </summary>
public class CacheKeyTests
{
    [Fact]
    public void SerializeDeserialize_SampleCacheKey_ShouldMatch()
    {
        // Arrange.
        var idColumn = new Column("id", DataType.Integer);
        var nameColumn = new Column("name", DataType.String);
        var cacheKey = new CacheKey(
            "github actions",
            new[] { idColumn.Name, nameColumn.Name },
            new QueryContextCondition[]
            {
                new(idColumn, VariantValue.Operation.GreaterOrEquals, new VariantValue(10)),
                new(nameColumn, VariantValue.Operation.Like, new VariantValue("%rob bob%")),
            },
            offset: 5,
            limit: 100);

        // Act.
        var str = cacheKey.Serialize();
        var deserializedCacheKey = CacheKey.Deserialize(str, idColumn, nameColumn);

        // Assert.
        Assert.Equal(
            "\"F:github actions\" S:id S:name O:5 L:100 W:id,103,i:10 \"W:name,110,s:\"\"%rob bob%\"\"\"", str);
        Assert.Equal(cacheKey.From, deserializedCacheKey.From);
        Assert.Equal(cacheKey.Conditions.Length, deserializedCacheKey.Conditions.Length);
        Assert.Equal(cacheKey.Conditions[0].Value, deserializedCacheKey.Conditions[0].Value);
    }
}
