using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal sealed class WindowFunctionsRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    public WindowFunctionsRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
        _rowsFrame = new RowsFrame(_rowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            FillRows();
            _isInitialized = true;
        }

        return _rowsFrameIterator.MoveNext();
    }

    private void FillRows()
    {
        while (_rowsIterator.MoveNext())
        {
            _rowsFrame.AddRow(_rowsIterator.Current);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isInitialized = false;
        _rowsFrame.Clear();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Window", _rowsIterator);
    }
}
