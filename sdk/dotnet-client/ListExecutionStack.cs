using System;
using System.Collections;
using System.Collections.Generic;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Simple stack implementation with list.
/// </summary>
public sealed class ListExecutionStack : IExecutionStack
{
    private int _position;
    private readonly List<VariantValue> _stack;
    private readonly Stack<int> _frames = new();
    private int _lastFramePosition;

    /// <inheritdoc />
    public VariantValue this[int index]
    {
        get => _lastFramePosition > -1 ? _stack[_lastFramePosition + index] : default;
        set => _stack[_lastFramePosition + index] = value;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public ListExecutionStack()
    {
        _stack = new List<VariantValue>();
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
        _position = _frames.Pop();
        _lastFramePosition = _frames.Count > 0 ? _frames.Peek() : 0;
    }

    /// <inheritdoc />
    public void Push(VariantValue value)
    {
        if (_stack.Count > _position)
        {
            _stack[_position] = value;
        }
        else
        {
            _stack.Add(value);
        }
        _position++;
    }

    /// <inheritdoc />
    public VariantValue Pop()
    {
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
}