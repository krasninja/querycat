using Xunit;
using QueryCat.Backend.Abstractions;
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
            from: "github actions",
            inputArguments: new[] { "1", "one two" },
            selectColumns: new[] { idColumn.Name, nameColumn.Name },
            conditions: new CacheKeyCondition[]
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
        Assert.Equal(cacheKey.From, deserializedCacheKey.From);
        Assert.Equal(cacheKey.Conditions.Count, deserializedCacheKey.Conditions.Count);
        Assert.True(cacheKey.Match(deserializedCacheKey));
    }

    [Fact]
    public void Match_SubsetCacheKey_ShouldMatch()
    {
        // Arrange.
        var idColumn = new Column("id", DataType.Integer);
        var nameColumn = new Column("name", DataType.String);
        var cacheKey1 = new CacheKey(
            from: "github actions",
            inputArguments: new[] { "1", "one two" },
            selectColumns: new[] { idColumn.Name, nameColumn.Name },
            conditions: new CacheKeyCondition[]
            {
                new(idColumn, VariantValue.Operation.GreaterOrEquals, new VariantValue(10)),
                new(nameColumn, VariantValue.Operation.Like, new VariantValue("%rob bob%")),
            },
            offset: 5,
            limit: 100);

        // Act.
        var cacheKey2 = new CacheKey(
            from: "github actions",
            inputArguments: new[] { "one two" },
            selectColumns: new[] { idColumn.Name },
            conditions: new CacheKeyCondition[]
            {
                new(nameColumn, VariantValue.Operation.Like, new VariantValue("%rob bob%")),
            },
            offset: 7,
            limit: 7);

        // Assert.
        Assert.True(cacheKey2.Match(cacheKey1));
    }
}
