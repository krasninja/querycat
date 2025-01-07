using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionValueGeneratorIterator : IKeyConditionMultipleValuesGenerator
{
    private readonly IRowsIterator _rowsIterator;
    private int _position = -1;

    /// <inheritdoc />
    public int Position => _position;

    public KeyConditionValueGeneratorIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var hasData = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (hasData)
        {
            _position++;
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _position = -1;
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public VariantValue Get(IExecutionThread thread)
        => _rowsIterator.Columns.Length > 0 ? _rowsIterator.Current[0] : VariantValue.Null;
}
