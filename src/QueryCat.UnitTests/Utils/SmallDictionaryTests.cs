using Xunit;
using QueryCat.Backend.Core.Utils;

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

    [Fact]
    public void Constructor_WithCapacity_ShouldHaveZeroCount()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>(10);

        // Assert.
        Assert.Empty(dict);
    }

    [Fact]
    public void Constructor_WithCapacity_ShouldNotResizeUntilExceeded()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>(4);

        // Act.
        dict.Add(1, "One");
        dict.Add(2, "Two");
        dict.Add(3, "Three");
        dict.Add(4, "Four");

        // Assert.
        Assert.Equal(4, dict.Count);
        Assert.Equal("Four", dict[4]);
    }

    [Fact]
    public void ContainsKey_ExistingKey_ShouldReturnTrue()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Assert.
        Assert.True(_dictionary.ContainsKey(1));
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ShouldReturnFalse()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Assert.
        Assert.False(_dictionary.ContainsKey(2));
    }

    [Fact]
    public void TryGetValue_ExistingKey_ShouldReturnTrueAndValue()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act.
        var found = _dictionary.TryGetValue(1, out var value);

        // Assert.
        Assert.True(found);
        Assert.Equal("One", value);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ShouldReturnFalse()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act.
        var found = _dictionary.TryGetValue(2, out var value);

        // Assert.
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange.
        _dictionary.Add(1, "One");
        _dictionary.Add(2, "Two");

        // Act.
        _dictionary.Clear();

        // Assert.
        Assert.Empty(_dictionary);
        Assert.False(_dictionary.ContainsKey(1));
    }

    [Fact]
    public void CopyTo_ShouldCopyAllItems()
    {
        // Arrange.
        _dictionary.Add(1, "One");
        _dictionary.Add(2, "Two");
        _dictionary.Add(3, "Three");
        var array = new KeyValuePair<int, string>[3];

        // Act.
        _dictionary.CopyTo(array, 0);

        // Assert.
        Assert.Equal(new KeyValuePair<int, string>(1, "One"), array[0]);
        Assert.Equal(new KeyValuePair<int, string>(2, "Two"), array[1]);
        Assert.Equal(new KeyValuePair<int, string>(3, "Three"), array[2]);
    }

    [Fact]
    public void CopyTo_WithOffset_ShouldCopyToCorrectPosition()
    {
        // Arrange.
        _dictionary.Add(1, "One");
        _dictionary.Add(2, "Two");
        var array = new KeyValuePair<int, string>[4];

        // Act.
        _dictionary.CopyTo(array, 2);

        // Assert.
        Assert.Equal(default, array[0]);
        Assert.Equal(default, array[1]);
        Assert.Equal(new KeyValuePair<int, string>(1, "One"), array[2]);
        Assert.Equal(new KeyValuePair<int, string>(2, "Two"), array[3]);
    }

    [Fact]
    public void Enumeration_ShouldReturnAllItems()
    {
        // Arrange.
        _dictionary.Add(1, "One");
        _dictionary.Add(2, "Two");
        _dictionary.Add(3, "Three");

        // Act.
        var items = new List<KeyValuePair<int, string>>();
        foreach (var item in _dictionary)
        {
            items.Add(item);
        }

        // Assert.
        Assert.Equal(3, items.Count);
        Assert.Equal(new KeyValuePair<int, string>(1, "One"), items[0]);
        Assert.Equal(new KeyValuePair<int, string>(2, "Two"), items[1]);
        Assert.Equal(new KeyValuePair<int, string>(3, "Three"), items[2]);
    }

    [Fact]
    public void Add_DuplicateKey_ShouldThrow()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act.
        var exception = Assert.Throws<ArgumentException>(() => _dictionary.Add(1, "Uno"));

        // Assert.
        Assert.Contains("same key", exception.Message);
    }

    [Fact]
    public void Keys_ShouldReturnOnlyAddedKeys()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>(10);
        dict.Add(1, "One");
        dict.Add(2, "Two");

        // Act.
        var keys = dict.Keys;

        // Assert.
        Assert.Equal(2, keys.Count);
        Assert.Contains(1, keys);
        Assert.Contains(2, keys);
        Assert.DoesNotContain(3, keys);
    }

    [Fact]
    public void Values_ShouldReturnOnlyAddedValues()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>(10);
        dict.Add(1, "One");
        dict.Add(2, "Two");

        // Act.
        var values = dict.Values;

        // Assert.
        Assert.Equal(2, values.Count);
        Assert.Contains("One", values);
        Assert.Contains("Two", values);
        Assert.DoesNotContain("Three", values);
    }

    [Fact]
    public void Keys_CopyTo_ShouldCopyOnlyAddedKeys()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>(10);
        dict.Add(1, "One");
        dict.Add(2, "Two");
        var array = new int[2];

        // Act.
        dict.Keys.CopyTo(array, 0);

        // Assert.
        Assert.Equal(1, array[0]);
        Assert.Equal(2, array[1]);
    }

    [Fact]
    public void Values_CopyTo_ShouldCopyOnlyAddedValues()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>(10);
        dict.Add(1, "One");
        dict.Add(2, "Two");
        var array = new string[2];

        // Act.
        dict.Values.CopyTo(array, 0);

        // Assert.
        Assert.Equal("One", array[0]);
        Assert.Equal("Two", array[1]);
    }

    [Fact]
    public void Indexer_GetNonExistingKey_ShouldThrow()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act & Assert.
        Assert.Throws<KeyNotFoundException>(() => _dictionary[2]);
    }

    [Fact]
    public void Contains_KeyValuePair_ShouldMatchBothKeyAndValue()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Assert.
        var containsExactMatch = _dictionary.Contains(new KeyValuePair<int, string>(1, "One"));
        var containsWrongValue = _dictionary.Contains(new KeyValuePair<int, string>(1, "Two"));
        var containsWrongKey = _dictionary.Contains(new KeyValuePair<int, string>(2, "One"));
        Assert.True(containsExactMatch);
        Assert.False(containsWrongValue);
        Assert.False(containsWrongKey);
    }

    [Fact]
    public void Remove_KeyValuePair_WithNonExistingKey_ShouldReturnFalse()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act.
        var result = _dictionary.Remove(new KeyValuePair<int, string>(2, "One"));

        // Assert.
        Assert.False(result);
    }

    [Fact]
    public void Remove_KeyValuePair_WithWrongValue_ShouldReturnFalse()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act.
        var result = _dictionary.Remove(new KeyValuePair<int, string>(1, "Two"));

        // Assert.
        Assert.False(result);
    }

    [Fact]
    public void Remove_KeyValuePair_WithExactMatch_ShouldThrow()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act & Assert.
        Assert.Throws<NotSupportedException>(
            () => _dictionary.Remove(new KeyValuePair<int, string>(1, "One")));
    }

    [Fact]
    public void Remove_Key_ShouldThrow()
    {
        // Arrange.
        _dictionary.Add(1, "One");

        // Act & Assert.
        Assert.Throws<NotSupportedException>(() => _dictionary.Remove(1));
    }

    [Fact]
    public void Enumeration_EmptyDictionary_ShouldReturnNoItems()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>();

        // Act.
        var count = 0;
        foreach (var item in dict)
        {
            _ = item;
            count++;
        }

        // Assert.
        Assert.Equal(0, count);
    }

    [Fact]
    public void Keys_IsReadOnly_ShouldReturnTrue()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>();

        // Assert.
        Assert.True(dict.Keys.IsReadOnly);
    }

    [Fact]
    public void Values_IsReadOnly_ShouldReturnTrue()
    {
        // Arrange.
        var dict = new SmallDictionary<int, string>();

        // Assert.
        Assert.True(dict.Values.IsReadOnly);
    }
}
