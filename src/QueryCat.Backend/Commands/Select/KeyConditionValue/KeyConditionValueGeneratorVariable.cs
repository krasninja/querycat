using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionValueGeneratorVariable : IKeyConditionMultipleValuesGenerator
{
    private readonly IFuncUnit _identifierUnit;
    private IKeyConditionMultipleValuesGenerator? _generator;

    /// <inheritdoc />
    public int Position => _generator?.Position ?? -1;

    public KeyConditionValueGeneratorVariable(IFuncUnit identifierUnit)
    {
        _identifierUnit = identifierUnit;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var generator = await GetGeneratorAsync(thread, cancellationToken);
        return await generator.MoveNextAsync(thread, cancellationToken);
    }

    private async ValueTask<IKeyConditionMultipleValuesGenerator> GetGeneratorAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        if (_generator != null)
        {
            return _generator;
        }
        var value = await _identifierUnit.InvokeAsync(thread, cancellationToken);
        var rowsIterator = RowsIteratorConverter.Convert(value);
        if (rowsIterator.Columns.Length < 1)
        {
            _generator = KeyConditionValueGeneratorEmpty.Instance;
            return _generator;
        }

        var values = new List<VariantValue>();
        while (await rowsIterator.MoveNextAsync(cancellationToken))
        {
            values.Add(rowsIterator.Current[0]);
        }
        await rowsIterator.ResetAsync(cancellationToken);

        _generator = new KeyConditionValueGeneratorArray(values);
        return _generator;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _generator?.Reset();
    }

    /// <inheritdoc />
    public bool TryGet(IExecutionThread thread, out VariantValue value)
    {
        if (_generator == null)
        {
            value = VariantValue.Null;
            return false;
        }
        return _generator.TryGet(thread, out value);
    }
}
