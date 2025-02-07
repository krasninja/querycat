using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

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
        AsyncUtils.RunSync(_rowsIterator.ResetAsync);
    }

    /// <inheritdoc />
    public ValueTask<VariantValue?> GetAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        if (_rowsIterator.Columns.Length > 0)
        {
            return ValueTask.FromResult(new VariantValue?(_rowsIterator.Current[0]));
        }
        return ValueTask.FromResult((VariantValue?)null);
    }
}
