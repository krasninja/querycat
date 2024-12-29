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

    public KeyConditionValueGeneratorArray(IEnumerable<VariantValue> values)
    {
        _values = values.Select(v => new FuncUnitStatic(v)).Cast<IFuncUnit>().ToArray();
    }

    /// <inheritdoc />
    public VariantValue Get(IExecutionThread thread)
        => _currentIndex < _values.Length ? _values[_currentIndex].Invoke(thread) : VariantValue.Null;

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var canMove = _currentIndex < _values.Length;
        if (canMove)
        {
            _currentIndex++;
        }
        canMove = _currentIndex < _values.Length;
        return ValueTask.FromResult(canMove);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _currentIndex = -1;
    }
}
