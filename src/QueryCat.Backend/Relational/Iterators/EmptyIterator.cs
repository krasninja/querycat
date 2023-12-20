using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

internal sealed class EmptyIterator : IRowsIterator
{
    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current { get; }

    public static EmptyIterator Instance { get; } = new();

    public EmptyIterator()
    {
        Columns = new[] { new Column("empty", DataType.Integer) };
        var frame = new RowsFrame(Columns);
        Current = new Row(frame);
        frame.AddRow(Current);
    }

    public EmptyIterator(IRowsSchema schema)
    {
        Columns = schema.Columns;
        Current = new Row(schema);
    }

    /// <inheritdoc />
    public bool MoveNext() => false;

    /// <inheritdoc />
    public void Reset()
    {
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Empty");
    }
}
