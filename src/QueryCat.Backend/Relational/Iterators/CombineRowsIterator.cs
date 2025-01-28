using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator combines multiple iterators with the same schema into a single sequence using
/// union, intersect and except methods.
/// </summary>
internal sealed class CombineRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _leftIterator;
    private readonly IRowsIterator _rightIterator;
    private readonly CombineType _combineType;
    private readonly bool _isDistinct;
    private readonly Func<CancellationToken, ValueTask<bool>> _moveDelegate;
    private readonly HashSet<VariantValueArray> _distinctValues = new();
    private IRowsIterator _currentIterator;

    // For intersect and except methods.
    private readonly HashSet<VariantValueArray> _rightRows = new();
    private bool _isRightInitialized;

    /// <inheritdoc />
    public Column[] Columns => _leftIterator.Columns;

    /// <inheritdoc />
    public Row Current => _currentIterator.Current;

    public CombineRowsIterator(IRowsIterator leftIterator, IRowsIterator rightIterator, CombineType combineType, bool isDistinct)
    {
        _leftIterator = leftIterator;
        _currentIterator = _leftIterator;
        _rightIterator = rightIterator;
        _combineType = combineType;
        _isDistinct = isDistinct;

        _moveDelegate = combineType switch
        {
            CombineType.Union => UnionDelegate,
            CombineType.Except => ExceptDelegate,
            CombineType.Intersect => IntersectDelegate,
            _ => throw new ArgumentException(string.Format(Resources.Errors.NotImplemented, combineType), nameof(combineType)),
        };

        if (!_leftIterator.IsSchemaEqual(_rightIterator))
        {
            throw new SemanticException(Resources.Errors.CombineMustHaveSameColumns);
        }
    }

    private async ValueTask<bool> UnionDelegate(CancellationToken cancellationToken)
    {
        var result = await _currentIterator.MoveNextAsync(cancellationToken);
        if (!result)
        {
            if (_currentIterator == _leftIterator)
            {
                _currentIterator = _rightIterator;
                result = await _currentIterator.MoveNextAsync(cancellationToken);
            }
        }

        return result;
    }

    private async ValueTask<bool> IntersectDelegate(CancellationToken cancellationToken)
    {
        await InitializedRightAsync(cancellationToken);

        while (await _currentIterator.MoveNextAsync(cancellationToken))
        {
            var arr = new VariantValueArray(_currentIterator.Current.AsArray(copy: false));
            if (_rightRows.Contains(arr))
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask<bool> ExceptDelegate(CancellationToken cancellationToken)
    {
        await InitializedRightAsync(cancellationToken);

        while (await _currentIterator.MoveNextAsync(cancellationToken))
        {
            var arr = new VariantValueArray(_currentIterator.Current.AsArray(copy: false));
            if (!_rightRows.Contains(arr))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_isDistinct)
        {
            while (await _moveDelegate.Invoke(cancellationToken))
            {
                var arr = new VariantValueArray(Current.AsArray(copy: true));

                if (_distinctValues.Add(arr))
                {
                    return true;
                }
            }

            return false;
        }
        else
        {
            return await _moveDelegate.Invoke(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _leftIterator.ResetAsync(cancellationToken);
        await _rightIterator.ResetAsync(cancellationToken);
        _distinctValues.Clear();
        _rightRows.Clear();
        _isRightInitialized = false;
        _currentIterator = _leftIterator;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent(
            $"Combine (type={_combineType}, distinct={_isDistinct})", _leftIterator, _rightIterator);
    }

    private async ValueTask InitializedRightAsync(CancellationToken cancellationToken)
    {
        if (_isRightInitialized)
        {
            return;
        }

        while (await _rightIterator.MoveNextAsync(cancellationToken))
        {
            var arr = new VariantValueArray(_rightIterator.Current.AsArray(copy: true));
            _rightRows.Add(arr);
        }

        _isRightInitialized = true;
        _currentIterator = _leftIterator;
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _leftIterator;
        yield return _rightIterator;
    }
}
