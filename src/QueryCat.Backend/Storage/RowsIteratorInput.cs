using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Create input from rows iterator.
/// </summary>
public sealed class RowsIteratorInput : IRowsInput, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly string _id;
    private QueryContext _queryContext = NullQueryContext.Instance;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public string[] UniqueKey { get; }

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _queryContext;
        set => _queryContext = value;
    }

    public RowsIteratorInput(IRowsIterator rowsIterator, string? id = null)
    {
        _rowsIterator = rowsIterator;
        Columns = rowsIterator.Columns;
        _id = id ?? Guid.NewGuid().ToString("N");
        UniqueKey = [_id];
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = _rowsIterator.Current[columnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
        => _rowsIterator.MoveNextAsync(cancellationToken);

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Iterator input (id={_id})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
