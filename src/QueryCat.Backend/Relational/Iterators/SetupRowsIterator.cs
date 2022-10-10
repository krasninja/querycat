using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator executes initialization delegates before rows processing.
/// </summary>
public class SetupRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly string _message;
    private bool _isInitialized;

    public virtual IRowsIterator RowsIterator => _rowsIterator;

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
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            BeforeInitialize(_rowsIterator);
            Initialize();
            _isInitialized = true;
            AfterInitialize(_rowsIterator);
        }

        return _rowsIterator.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isInitialized = false;
    }

    /// <inheritdoc />
    public virtual void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Setup (msg='{_message}' init={_isInitialized})", _rowsIterator);
    }
}
