using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Relational;

public abstract class ClassRowsInput<TClass> : RowsInput where TClass : class
{
    private ClassRowsFrame<TClass> _data = ClassRowsFrame<TClass>.Empty;
    private IRowsIterator? _dataIterator;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    public abstract ClassRowsFrame<TClass> CreateRowsFrame(QueryContext queryContext);

    public abstract void FillRowsFrame(ClassRowsFrame<TClass> data, QueryContext queryContext);

    /// <inheritdoc />
    public override void Open()
    {
        Columns = _data.Columns;
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        _data = CreateRowsFrame(QueryContext);
    }

    /// <inheritdoc />
    public override void Close()
    {
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_dataIterator == null)
        {
            throw new InvalidOperationException("Data iterator is not initialized, call ReadNext.");
        }
        value = _dataIterator.Current[columnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override bool ReadNext()
    {
        if (_dataIterator == null)
        {
            FillRowsFrame(_data, QueryContext);
            _dataIterator = _data.GetIterator();
        }

        return _dataIterator.MoveNext();
    }
}
