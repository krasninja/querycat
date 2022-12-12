using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator combines multiple iterators with the same schema into a single sequence.
/// </summary>
internal sealed class CombineRowsIterator : IRowsIterator
{
    /// <inheritdoc />
    public Column[] Columns { get; private set; } = Array.Empty<Column>();

    /// <inheritdoc />
    public Row Current
    {
        get
        {
            if (_currentRowIteratorNode == null)
            {
                throw new InvalidOperationException("Rows iterator is not initialized.");
            }
            return _currentRowIteratorNode.Value.Current;
        }
    }

    private LinkedListNode<IRowsIterator>? _currentRowIteratorNode;

    private bool _firstMoveCall = true;

    private readonly LinkedList<IRowsIterator> _rowIterators = new();

    public IReadOnlyCollection<IRowsIterator> RowIterators => _rowIterators;

    private bool AreColumnsInitialized => _currentRowIteratorNode != null;

    public void AddRowsIterator(IRowsIterator rowsIterator)
    {
        if (rowsIterator == null)
        {
            throw new ArgumentNullException(nameof(rowsIterator));
        }

        // Make sure the columns schema is valid.
        if (AreColumnsInitialized)
        {
            if (rowsIterator.Columns.Length != Columns.Length)
            {
                throw new SemanticException("Each UNION query must have the same number of columns.");
            }
            for (var i = 0; i < Columns.Length; i++)
            {
                if (!DataTypeUtils.EqualsWithCast(rowsIterator.Columns[i].DataType, Columns[i].DataType))
                {
                    throw new SemanticException($"Types mismatch for column {Columns[i].Name}.");
                }
            }
        }

        _rowIterators.AddLast(rowsIterator);
        if (_currentRowIteratorNode == null)
        {
            _currentRowIteratorNode = _rowIterators.First;
            Columns = rowsIterator.Columns;
        }
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_firstMoveCall && _currentRowIteratorNode != null)
        {
            _firstMoveCall = false;
        }

        if (_currentRowIteratorNode == null)
        {
            return false;
        }

        if (_currentRowIteratorNode.Value.MoveNext())
        {
            return true;
        }

        _currentRowIteratorNode = _currentRowIteratorNode.Next;
        return MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _firstMoveCall = true;
        foreach (var rowsIterator in _rowIterators)
        {
            rowsIterator.Reset();
        }
        _currentRowIteratorNode = _rowIterators.First;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Combine", _rowIterators.ToArray());
    }
}
