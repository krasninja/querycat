using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Allows to create rows input from .NET objects.
/// </summary>
/// <typeparam name="TClass">Object class.</typeparam>
public abstract class ClassRowsInput<TClass> : RowsInput where TClass : class
{
    private ClassRowsFrame<TClass>? _frame;
    private IRowsIterator? _dataIterator;

    /// <summary>
    /// Source frame.
    /// </summary>
    protected ClassRowsFrame<TClass> Frame => _frame ??
        throw new InvalidOperationException("The frame is not initialized.");

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    /// <summary>
    /// Create rows frame.
    /// </summary>
    /// <param name="builder">Rows frame builder.</param>
    protected abstract void Initialize(ClassRowsFrameBuilder<TClass> builder);

    /// <inheritdoc />
    public override void Open()
    {
        var builder = new ClassRowsFrameBuilder<TClass>();
        Initialize(builder);
        _frame = builder.BuildRowsFrame();

        Columns = Frame.Columns;
    }

    /// <inheritdoc />
    protected override void Load()
    {
        if (_dataIterator == null)
        {
            _dataIterator = Frame.GetIterator();
            base.Load();
        }
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
            Load();
            _dataIterator = Frame.GetIterator();
        }

        return _dataIterator.MoveNext();
    }
}
