using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational.Iterators;

public sealed class SingleValueRowsIterator : IRowsIterator
{
    public const string ColumnTitle = "value";

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current { get; }

    private bool _isIterated;

    public SingleValueRowsIterator()
    {
        Columns = new[]
        {
            new Column(ColumnTitle, DataType.Void)
        };
        Current = new Row(this)
        {
            [0] = VariantValue.Null
        };
    }

    public SingleValueRowsIterator(VariantValue value)
    {
        Columns =
        [
            new Column(ColumnTitle, value.Type)
        ];
        Current = new Row(this)
        {
            [0] = value
        };
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_isIterated)
        {
            return false;
        }
        _isIterated = true;
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isIterated = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Single");
    }
}
