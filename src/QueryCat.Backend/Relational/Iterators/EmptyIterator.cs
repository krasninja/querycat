using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

public sealed class EmptyIterator : IRowsIterator
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
