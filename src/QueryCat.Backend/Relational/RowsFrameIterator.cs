using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Iterator for <see cref="RowsFrame" />. The iterator skips removed rows.
/// </summary>
public sealed class RowsFrameIterator : ICursorRowsIterator
{
    private const int InitialCursorPosition = -1;

    private readonly RowsFrame _rowsFrame;
    private readonly IRowsIterator? _childIterator;
    private readonly Row _currentRow;
    private int _absoluteCursor = InitialCursorPosition;
    private int _relativeCursor = -1;

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    public RowsFrame RowsFrame => _rowsFrame;

    /// <inheritdoc />
    public int Position => _absoluteCursor;

    /// <inheritdoc />
    public int TotalRows => _rowsFrame.TotalActiveRows;

    /// <inheritdoc />
    public Row Current
    {
        get
        {
            _rowsFrame.ReadRowAt(_currentRow, _absoluteCursor);
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
        ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));

        if (origin == CursorSeekOrigin.End)
        {
            offset = _rowsFrame.TotalActiveRows - offset - 1;
        }

        if (_rowsFrame.TotalRows == _rowsFrame.TotalActiveRows)
        {
            if (origin == CursorSeekOrigin.Begin)
            {
                _absoluteCursor = offset;
            }
            else if (origin == CursorSeekOrigin.Current)
            {
                _absoluteCursor += offset;
            }
            _relativeCursor = _absoluteCursor;
        }
        else
        {
            if (origin == CursorSeekOrigin.Begin || origin == CursorSeekOrigin.End)
            {
                _relativeCursor = -1;
                _absoluteCursor = -1;
            }
            while (_relativeCursor < offset && _absoluteCursor < _rowsFrame.TotalRows)
            {
                _absoluteCursor++;
                if (!_rowsFrame.IsRemoved(_absoluteCursor))
                {
                    _relativeCursor++;
                }
            }
        }
    }

    internal bool MoveNext()
    {
        do
        {
            if (!HasData)
            {
                break;
            }
            _absoluteCursor++;
        }
        while (_rowsFrame.IsRemoved(_absoluteCursor));

        return HasData;
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(MoveNext());
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _absoluteCursor = InitialCursorPosition;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Whether iterator has data.
    /// </summary>
    public bool HasData => _rowsFrame.TotalRows >= _absoluteCursor + 1 && _rowsFrame.TotalRows > 0;

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
