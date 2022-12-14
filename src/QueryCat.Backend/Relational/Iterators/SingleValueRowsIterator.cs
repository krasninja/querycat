using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

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
        Columns = new[]
        {
            new Column(ColumnTitle, value.GetInternalType())
        };
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
