using Xunit;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="DefaultObjectSelector" />.
/// </summary>
public class DefaultObjectSelectorTests
{
    private readonly DefaultObjectSelector _selector = new();

    [Fact]
    public void SelectByIndex_Array_ShouldReturnCorrectValueByIndex()
    {
        // Arrange.
        var target = new[] { 1, 2, 3 };
        var context = new ObjectSelectorContext(target);

        // Act.
        var token = _selector.SelectByIndex(context, 1)!.Value;

        Assert.Equal(2, token.Value);
    }

    [Fact]
    public void SetValue_Array_SetCorrectly()
    {
        // Arrange.
        var target = new[] { 1, 2, 3 };
        var context = new ObjectSelectorContext(target);
        context.Push(new ObjectSelectorContext.Token(target, Indexes: [1]));

        // Act.
        _selector.SetValue(context, 5);

        Assert.Equal(5, target[1]);
    }

    [Fact]
    public void SelectByIndex_Dictionary_ShouldReturnCorrectValueByIndex()
    {
        // Arrange.
        var target = new Dictionary<string, int>
        {
            ["item1"] = 10,
            ["item2"] = 25,
        };
        var context = new ObjectSelectorContext(target);

        // Act.
        var token = _selector.SelectByIndex(context, "item2")!.Value;

        Assert.Equal(25, token.Value);
    }

    [Fact]
    public void SetValue_Dictionary_SetCorrectly()
    {
        // Arrange.
        var target = new Dictionary<string, int>
        {
            ["item1"] = 10,
            ["item2"] = 25,
        };
        var context = new ObjectSelectorContext(target);
        context.Push(new ObjectSelectorContext.Token(target, Indexes: ["item2"]));

        // Act.
        _selector.SetValue(context, 13);

        Assert.Equal(13, target["item2"]);
    }
}
