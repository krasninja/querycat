using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Implements algebraic multiply between two rows iterators.
/// </summary>
internal sealed class MultiplyRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _leftRowsIterator;
    private readonly IRowsIterator _rightRowsIterator;
    private readonly Row _currentRow;
    private Row? _currentLeftRow;
    private readonly IRowsIterator _currentLeftIterator;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _currentRow;

    public MultiplyRowsIterator(IRowsIterator leftRowsIterator, IRowsIterator rightRowsIterator)
    {
        var leftRowsFrame = new RowsFrame(leftRowsIterator.Columns);
        _leftRowsIterator = leftRowsIterator;
        _currentLeftIterator = leftRowsIterator;
        _rightRowsIterator = rightRowsIterator;

        Columns = leftRowsFrame.Columns.Concat(_rightRowsIterator.Columns).ToArray();
        _currentRow = new Row(this);
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_currentLeftRow == null)
        {
            var leftHasNext = await _currentLeftIterator.MoveNextAsync(cancellationToken);
            if (!leftHasNext)
            {
                return false;
            }
            _currentLeftRow = _currentLeftIterator.Current;
        }

        var rightHasNext = await _rightRowsIterator.MoveNextAsync(cancellationToken);
        if (rightHasNext)
        {
            _currentLeftIterator.Current.Copy(_currentRow);
            _rightRowsIterator.Current.Copy(0, _currentRow, _currentLeftIterator.Columns.Length);
            return true;
        }

        await _rightRowsIterator.ResetAsync(cancellationToken);
        _currentLeftRow = null;
        return await MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rightRowsIterator.ResetAsync(cancellationToken);
        await _leftRowsIterator.ResetAsync(cancellationToken);
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
