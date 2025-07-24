using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

internal sealed class ListRowsIterator : IRowsIterator
{
    private readonly IReadOnlyList<VariantValue> _values;
    private readonly Row _currentRow;
    private int _currentIndex = -1;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _currentRow;

    public ListRowsIterator(IReadOnlyList<VariantValue> values, DataType? dataType = null)
    {
        _values = values;

        // Determine type.
        if (dataType == null)
        {
            dataType = DataType.String;
            foreach (var value in _values)
            {
                if (!value.IsNull)
                {
                    dataType = value.Type;
                    break;
                }
            }
        }

        // Determine type.
        Columns =
        [
            new Column(Column.ValueColumnTitle, dataType.Value),
        ];
        _currentRow = new Row(Columns);
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        _currentIndex++;
        var isInRange = _currentIndex > -1 && _currentIndex < _values.Count;
        _currentRow[0] = isInRange ? _values[_currentIndex] : VariantValue.Null;
        return ValueTask.FromResult(isInRange);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _currentIndex = -1;
        _currentRow[0] = VariantValue.Null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"List (length={_values.Count})");
    }
}
