using Xunit;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="DefaultFixedSizeExecutionStack" />.
/// </summary>
public sealed class DefaultFixedSizeExecutionStackTests
{
    private readonly DefaultFixedSizeExecutionStack _stack = new();

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
        Assert.Equal(2, _stack[1].AsInteger!.Value);
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
        Assert.Equal(4, _stack[0].AsInteger!.Value);
    }

    [Fact]
    public void Close_FrameWithArguments_ShouldBeZero()
    {
        // Act.
        _stack.CreateFrame();
        _stack.Push(new VariantValue(1));
        _stack.Push(new VariantValue(2));
        _stack.Push(new VariantValue(3));
        _stack.CloseFrame();

        // Assert.
        Assert.Equal(0, _stack.FrameLength);
    }

    [Fact]
    public void Enumerable_FrameWithArguments_ShouldBeTheSame()
    {
        // Act.
        _stack.CreateFrame();
        _stack.Push(new VariantValue(1));
        _stack.Push(new VariantValue(2));
        _stack.Push(new VariantValue(3));
        var stackCopy = _stack.ToArray();
        _stack.CloseFrame();

        // Assert.
        Assert.Equal(3, stackCopy.Length);
    }
}
