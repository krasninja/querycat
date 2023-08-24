using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Implements algebraic multiply between two rows iterators.
/// </summary>
internal sealed class MultiplyRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _leftRowsIterator;
    private readonly IRowsIterator _rightRowsIterator;
    private readonly RowsFrame _leftRowsFrame;
    private readonly RowsFrameIterator _leftRowsFrameIterator;
    private readonly Row _currentRow;
    private Row? _currentRightRow;
    private IRowsIterator _currentLeftIterator;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _currentRow;

    public MultiplyRowsIterator(IRowsIterator leftRowsIterator, IRowsIterator rightRowsIterator)
    {
        _leftRowsFrame = new RowsFrame(leftRowsIterator.Columns);
        _leftRowsFrameIterator = _leftRowsFrame.GetIterator();
        _leftRowsIterator = leftRowsIterator;
        _currentLeftIterator = leftRowsIterator;
        _rightRowsIterator = rightRowsIterator;

        Columns = _leftRowsFrame.Columns.Concat(_rightRowsIterator.Columns).ToArray();
        _currentRow = new Row(this);
    }

    private bool SetNextRightRow()
    {
        if (!_rightRowsIterator.MoveNext())
        {
            return false;
        }
        _currentRightRow = _rightRowsIterator.Current;
        return true;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_currentRightRow == null)
        {
            if (!SetNextRightRow())
            {
                return false;
            }
        }

        var leftHasNext = _currentLeftIterator.MoveNext();

        // Left rows set has no rows.
        if (!leftHasNext && _leftRowsFrame.IsEmpty)
        {
            return false;
        }
        // On first iteration we fill row frame, then we reuse.
        if (leftHasNext && _currentLeftIterator != _leftRowsFrameIterator)
        {
            _leftRowsFrame.AddRow(_leftRowsIterator.Current);
        }
        // If there are no rows - reset and start again.
        if (!leftHasNext)
        {
            _currentLeftIterator = _leftRowsFrameIterator;
            if (!SetNextRightRow())
            {
                return false;
            }
            _leftRowsFrameIterator.Reset();
            _leftRowsFrameIterator.MoveNext();
        }
        Row.Copy(_currentLeftIterator.Current, _currentRow);
        Row.Copy(_currentRightRow!, 0, _currentRow, _currentLeftIterator.Columns.Length);

        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rightRowsIterator.Reset();
        _leftRowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Multiply", _leftRowsIterator, _rightRowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _leftRowsIterator;
        yield return _rightRowsIterator;
    }
}
