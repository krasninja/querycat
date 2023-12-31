using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator allows to create instance of <see cref="RowsFrame" /> right before MoveNext call.
/// </summary>
internal sealed class LazySetupRowsFrameIterator : SetupRowsIterator
{
    private readonly Action<RowsFrame, RowsFrameIterator> _rowsFrameFactory;
    private readonly RowsFrame _rowsFrame;

    public RowsFrame RowsFrame => _rowsFrame;

    public LazySetupRowsFrameIterator(
        Column[] columns,
        Action<RowsFrame, RowsFrameIterator> rowsFrameFactory)
        : base(new RowsFrame(columns).GetIterator(), "create frame")
    {
        _rowsFrameFactory = rowsFrameFactory;
        _rowsFrame = ((RowsFrameIterator)RowsIterator).RowsFrame;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        _rowsFrameFactory.Invoke(_rowsFrame, (RowsFrameIterator)RowsIterator);
        base.Initialize();
    }

    /// <inheritdoc />
    public override void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Lazy Setup");
    }
}
