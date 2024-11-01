using Xunit;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="SmallDictionary{TKey,TValue}" />.
/// </summary>
public class SmallDictionaryTests
{
    private readonly SmallDictionary<int, string> _dictionary = new();

    [Fact]
    public void Add_MultipleValue_Success()
    {
        // Act.
        _dictionary.Add(1, "One");
        _dictionary.Add(2, "Two");
        _dictionary.Add(3, "Three");
        _dictionary.Add(4, "Four");
        _dictionary.Add(5, "Five");

        // Assert.
        Assert.Equal("Three", _dictionary[3]);
    }

    [Fact]
    public void Set_MultipleValue_Success()
    {
        // Act.
        _dictionary.Add(1, "One");
        _dictionary.Add(2, "Two");
        _dictionary.Add(3, "Three");
        _dictionary.Add(4, "Four");
        _dictionary.Add(5, "Five");
        _dictionary[3] = "Tri";

        // Assert.
        Assert.Equal("Tri", _dictionary[3]);
    }
}
