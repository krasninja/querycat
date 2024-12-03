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
    public bool MoveNext(IExecutionThread thread) => GetGenerator(thread).MoveNext(thread);

    private IKeyConditionMultipleValuesGenerator GetGenerator(IExecutionThread thread)
    {
        if (_generator != null)
        {
            return _generator;
        }
        var value = _identifierUnit.Invoke(thread);
        var rowsIterator = RowsIteratorConverter.Convert(value);
        if (rowsIterator.Columns.Length < 1)
        {
            _generator = KeyConditionValueGeneratorEmpty.Instance;
            return _generator;
        }

        var values = new List<VariantValue>();
        while (rowsIterator.MoveNext())
        {
            values.Add(rowsIterator.Current[0]);
        }
        rowsIterator.Reset();

        _generator = new KeyConditionValueGeneratorArray(values);
        return _generator;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _generator?.Reset();
    }

    /// <inheritdoc />
    public VariantValue Get(IExecutionThread thread)
    {
        if (_generator == null)
        {
            return VariantValue.Null;
        }
        return _generator.Get(thread);
    }
}
