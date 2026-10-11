using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator maps input columns into another set of columns.
/// </summary>
internal sealed class MapRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly List<int> _mapping = [];
    private readonly List<DataType> _mappingTypesCast = [];
    private Column[] _columns = [];
    private Row _row;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public Row Current => _row;

    public MapRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
        _row = new Row(_columns);
    }

    /// <summary>
    /// Add column for source rows iterator to target.
    /// </summary>
    /// <param name="index">Source column index.</param>
    /// <param name="targetType">Target column type to apply cast.</param>
    /// <returns>Instance of <see cref="MapRowsIterator" />.</returns>
    public MapRowsIterator Add(int index, DataType? targetType = null)
    {
        Array.Resize(ref _columns, _columns.Length + 1);
        var column = _rowsIterator.Columns[index];
        _mapping.Add(index);
        _columns[^1] = column;
        _mappingTypesCast.Add(targetType ?? column.DataType);
        _row = new Row(_columns);
        return this;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        var hasData = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (!hasData)
        {
            return false;
        }
        for (var i = 0; i < _mapping.Count; i++)
        {
            var mappedIndex = _mapping[i];
            if (_rowsIterator.Columns[mappedIndex].DataType != _mappingTypesCast[i])
            {
                var value = _rowsIterator.Current[mappedIndex];
                if (value.TryCast(_mappingTypesCast[i], out var outValue))
                {
                    _row[i] = outValue;
                }
                else
                {
                    _row[i] = VariantValue.Null;
                }
            }
            else
            {
                _row[i] = _rowsIterator.Current[mappedIndex];
            }
        }
        return hasData;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _rowsIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Map", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
