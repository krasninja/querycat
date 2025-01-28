using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator executes initialization delegates before rows processing.
/// </summary>
internal class SetupRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly string _message;
    private bool _isInitialized;

    public IRowsIterator RowsIterator => _rowsIterator;

    /// <inheritdoc />
    public virtual Column[] Columns => _rowsIterator.Columns;

    public Action<IRowsIterator> BeforeInitialize { get; set; } = _ => { };

    public Action<IRowsIterator> AfterInitialize { get; set; } = _ => { };

    /// <inheritdoc />
    public virtual Row Current => _rowsIterator.Current;

    public SetupRowsIterator(IRowsIterator rowsIterator, string message)
    {
        _rowsIterator = rowsIterator;
        _message = message;
    }

    public virtual void Initialize()
    {
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            BeforeInitialize(_rowsIterator);
            Initialize();
            _isInitialized = true;
            AfterInitialize(_rowsIterator);
        }

        return await _rowsIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isInitialized = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Setup (msg='{_message}' init={_isInitialized})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
