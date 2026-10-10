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
    private readonly int _leftColumnsCount;
    private Row? _currentLeftRow;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _currentRow;

    public MultiplyRowsIterator(IRowsIterator leftRowsIterator, IRowsIterator rightRowsIterator)
    {
        _leftRowsIterator = leftRowsIterator;
        _rightRowsIterator = rightRowsIterator;
        _leftColumnsCount = leftRowsIterator.Columns.Length;

        Columns = leftRowsIterator.Columns.Concat(rightRowsIterator.Columns).ToArray();
        _currentRow = new Row(this);
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (_currentLeftRow == null)
            {
                if (!await _leftRowsIterator.MoveNextAsync(cancellationToken))
                {
                    return false;
                }
                _currentLeftRow = _leftRowsIterator.Current;
            }

            if (await _rightRowsIterator.MoveNextAsync(cancellationToken))
            {
                _currentLeftRow.Copy(_currentRow);
                _rightRowsIterator.Current.Copy(0, _currentRow, _leftColumnsCount);
                return true;
            }

            await _rightRowsIterator.ResetAsync(cancellationToken);
            _currentLeftRow = null;
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rightRowsIterator.ResetAsync(cancellationToken);
        await _leftRowsIterator.ResetAsync(cancellationToken);
        _currentLeftRow = null;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent(
            $"Multiply (left={_leftRowsIterator.Columns.Length}, right={_rightRowsIterator.Columns.Length}",
            _leftRowsIterator,
            _rightRowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _leftRowsIterator;
        yield return _rightRowsIterator;
    }
}
