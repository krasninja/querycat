using QueryCat.Backend.Core.Data;
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
        IRowsIterator rowsIterator;
        if (value.Type == DataType.Object
            && value.AsObjectUnsafe is IRowsInput rowsInput)
        {
            await rowsInput.OpenAsync(cancellationToken);
            rowsIterator = new RowsInputIterator(rowsInput);
        }
        else
        {
            rowsIterator = RowsIteratorConverter.Convert(value);
        }

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
    public ValueTask ResetAsync(CancellationToken cancellationToken)
    {
        if (_generator == null)
        {
            return ValueTask.CompletedTask;
        }
        return _generator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<VariantValue?> GetAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        if (_generator == null)
        {
            return ValueTask.FromResult((VariantValue?)null);
        }
        return _generator.GetAsync(thread, cancellationToken);
    }
}
