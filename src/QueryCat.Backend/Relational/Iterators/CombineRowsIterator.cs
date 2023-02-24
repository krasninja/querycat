using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

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
    private readonly Func<bool> _moveDelegate;
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
            _ => throw new ArgumentException($"{combineType} is not implemented.", nameof(combineType)),
        };

        if (!_leftIterator.IsSchemaEqual(_rightIterator))
        {
            throw new SemanticException("Each combine query must have equal columns count and types.");
        }
    }

    private bool UnionDelegate()
    {
        var result = _currentIterator.MoveNext();
        if (!result)
        {
            if (_currentIterator == _leftIterator)
            {
                _currentIterator = _rightIterator;
                result = _currentIterator.MoveNext();
            }
        }

        return result;
    }

    private bool IntersectDelegate()
    {
        InitializedRight();

        while (_currentIterator.MoveNext())
        {
            var arr = new VariantValueArray(_currentIterator.Current.AsArray(copy: false));
            if (_rightRows.Contains(arr))
            {
                return true;
            }
        }

        return false;
    }

    private bool ExceptDelegate()
    {
        InitializedRight();

        while (_currentIterator.MoveNext())
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
    public bool MoveNext()
    {
        if (_isDistinct)
        {
            while (_moveDelegate.Invoke())
            {
                var arr = new VariantValueArray(Current.AsArray(copy: true));

                if (!_distinctValues.Contains(arr))
                {
                    _distinctValues.Add(arr);
                    return true;
                }
            }

            return false;
        }
        else
        {
            return _moveDelegate.Invoke();
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _leftIterator.Reset();
        _rightIterator.Reset();
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

    private void InitializedRight()
    {
        if (_isRightInitialized)
        {
            return;
        }

        while (_rightIterator.MoveNext())
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
