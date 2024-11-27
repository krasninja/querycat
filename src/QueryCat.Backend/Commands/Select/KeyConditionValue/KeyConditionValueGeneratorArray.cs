using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionValueGeneratorArray : IKeyConditionMultipleValuesGenerator
{
    private readonly IFuncUnit[] _values;
    private int _currentIndex = -1;

    /// <inheritdoc />
    public int Position => _currentIndex;

    public KeyConditionValueGeneratorArray(params IFuncUnit[] values)
    {
        _values = values;
    }

    /// <inheritdoc />
    public VariantValue Get(IExecutionThread thread)
        => _currentIndex < _values.Length ? _values[_currentIndex].Invoke(thread) : VariantValue.Null;

    /// <inheritdoc />
    public bool MoveNext()
    {
        var canMove = _currentIndex < _values.Length;
        if (canMove)
        {
            _currentIndex++;
        }
        canMove = _currentIndex < _values.Length;
        return canMove;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _currentIndex = -1;
    }
}
