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
    public Column[] Columns => _rowsIterator.Columns;

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
        _id = id ?? Guid.NewGuid().ToString("N");
        UniqueKey = [_id];
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
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
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _rowsIterator.ResetAsync(cancellationToken);
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
