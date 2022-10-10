using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Iterator for <see cref="RowsFrame" />.
/// </summary>
public sealed class RowsFrameIterator : ICursorRowsIterator
{
    private const int InitialCursorPosition = -1;

    private readonly RowsFrame _rowsFrame;
    private readonly IRowsIterator? _childIterator;
    private readonly Row _currentRow;
    private int _cursor = InitialCursorPosition;

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    public RowsFrame RowsFrame => _rowsFrame;

    /// <inheritdoc />
    public int Position => _cursor;

    /// <inheritdoc />
    public int TotalRows => _rowsFrame.TotalRows;

    /// <inheritdoc />
    public Row Current
    {
        get
        {
            _rowsFrame.ReadRowAt(_currentRow, _cursor);
            return _currentRow;
        }
    }

    public RowsFrameIterator(RowsFrame rowsFrame, IRowsIterator? childIterator = null)
    {
        _rowsFrame = rowsFrame;
        _childIterator = childIterator;
        _currentRow = new Row(this);
    }

    /// <inheritdoc />
    public void Seek(int offset, CursorSeekOrigin origin)
    {
        if (origin == CursorSeekOrigin.Begin)
        {
            _cursor = offset;
        }
        else if (origin == CursorSeekOrigin.Current)
        {
            _cursor += offset;
        }
        else if (origin == CursorSeekOrigin.End)
        {
            _cursor = TotalRows - offset;
        }
    }

    /// <summary>
    /// Reset cursor position, move to the begin of rows set.
    /// </summary>
    public void Reset()
    {
        _cursor = InitialCursorPosition;
    }

    /// <summary>
    /// Whether iterator has data.
    /// </summary>
    public bool HasData => _rowsFrame.TotalRows >= _cursor + 1 && _rowsFrame.TotalRows > 0;

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!HasData)
        {
            return false;
        }
        _cursor++;
        return HasData;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Rows Frame (total={_rowsFrame.TotalRows})");
        if (_childIterator != null)
        {
            stringBuilder.IncreaseIndent();
            _childIterator.Explain(stringBuilder);
            stringBuilder.DecreaseIndent();
        }
    }
}
