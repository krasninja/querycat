using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator combines multiple iterators with the same schema into a single sequence using
/// union, intersect and except methods.
/// </summary>
internal sealed class CombineRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _leftIterator;
    private readonly IRowsIterator _rightIterator;
    private readonly bool _isDistinct;
    private readonly Func<bool> _moveDelegate;
    private readonly HashSet<VariantValueArray> _values = new();
    private IRowsIterator _currentIterator;

    /// <inheritdoc />
    public Column[] Columns => _leftIterator.Columns;

    /// <inheritdoc />
    public Row Current => _currentIterator.Current;

    public CombineRowsIterator(IRowsIterator leftIterator, IRowsIterator rightIterator, CombineType combineType, bool isDistinct)
    {
        _leftIterator = leftIterator;
        _currentIterator = _leftIterator;
        _rightIterator = rightIterator;
        _isDistinct = isDistinct;

        _moveDelegate = combineType switch
        {
            CombineType.Union => UnionDelegate,
            CombineType.Except => ExceptDelegate,
            CombineType.Intersect => IntersectDelegate,
            _ => throw new NotImplementedException($"{combineType} is not implemented."),
        };

        if (!_leftIterator.IsSchemaEqual(_rightIterator, withSourceName: false))
        {
            throw new SemanticException("Each combine query must have equal columns names and types.");
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
        // TODO:
        return false;
    }

    private bool ExceptDelegate()
    {
        // TODO:
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

                if (!_values.Contains(arr))
                {
                    _values.Add(arr);
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
        _currentIterator = _leftIterator;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Combine", _leftIterator, _rightIterator);
    }
}
