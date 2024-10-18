using Xunit;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Client;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="ListExecutionStack" />.
/// </summary>
public class ListExecutionStackTests
{
    private readonly ListExecutionStack _stack = new();

    [Fact]
    public void Push_FrameWithArguments_ShouldHaveCorrectLength()
    {
        // Act.
        _stack.CreateFrame();
        _stack.Push(new VariantValue(1));
        _stack.Push(new VariantValue(2));
        _stack.Push(new VariantValue(3));

        // Assert.
        Assert.Equal(3, _stack.FrameLength);
        Assert.Equal(2, _stack[1]);
    }

    [Fact]
    public void Push_MultipleFramesWithArguments_ShouldHaveCorrectLength()
    {
        // Act.
        _stack.CreateFrame();
        _stack.Push(new VariantValue(-1));
        _stack.CloseFrame();
        _stack.CreateFrame();
        _stack.Push(new VariantValue(1));
        _stack.Push(new VariantValue(2));
        _stack.Push(new VariantValue(3));
        _stack.CreateFrame();
        _stack.Push(new VariantValue(4));
        _stack.Push(new VariantValue(5));

        // Assert.
        Assert.Equal(2, _stack.FrameLength);
        Assert.Equal(4, _stack[0]);
    }
}
