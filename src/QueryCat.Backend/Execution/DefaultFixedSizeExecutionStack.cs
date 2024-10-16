using System.Collections;
using System.Diagnostics;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Fixed size execution stack implementation.
/// </summary>
public sealed class DefaultFixedSizeExecutionStack : IExecutionStack
{
    private int _position;
    private readonly VariantValue[] _stack;
    private readonly Stack<int> _frames = new();
    private int _lastFramePosition;

    /// <inheritdoc />
    public VariantValue this[int index]
    {
        get
        {
            ValidateFrame();
            return _lastFramePosition > -1 ? _stack[_lastFramePosition + index] : default;
        }

        set
        {
            ValidateFrame();
            _stack[_lastFramePosition + index] = value;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="maxStackSize">Max stack size.</param>
    public DefaultFixedSizeExecutionStack(int maxStackSize = 1024)
    {
        _stack = new VariantValue[maxStackSize];
    }

    /// <inheritdoc />
    public int FrameLength => _position - _lastFramePosition;

    /// <inheritdoc />
    public ExecutionStackFrame CreateFrame()
    {
        _frames.Push(_position);
        _lastFramePosition = _position;
        return new ExecutionStackFrame(this);
    }

    /// <inheritdoc />
    public void CloseFrame()
    {
        ValidateFrame();
        _position = _frames.Pop();
        _lastFramePosition = _frames.Count > 0 ? _frames.Peek() : 0;
    }

    /// <inheritdoc />
    public void Push(VariantValue value)
    {
        ValidateFrame();
        _stack[_position++] = value;
    }

    /// <inheritdoc />
    public VariantValue Pop()
    {
        ValidateFrame();
        if (FrameLength == 0)
        {
            ArgumentOutOfRangeException.ThrowIfZero(FrameLength);
        }
        return _stack[--_position];
    }

    /// <inheritdoc />
    public IEnumerator<VariantValue> GetEnumerator()
    {
        for (var i = _lastFramePosition; i < _position; i++)
        {
            yield return _stack[i];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Conditional("DEBUG")]
    private void ValidateFrame()
    {
        Debug.Assert(_frames.Count > 0, "Stack has no frames.");
    }
}
