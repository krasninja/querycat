using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Iterator with no implementation.
/// </summary>
public sealed class EmptyRowsIterator : IRowsIterator
{
    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current { get; }

    /// <summary>
    /// Instance of <see cref="EmptyRowsIterator" />.
    /// </summary>
    public static EmptyRowsIterator Instance { get; } = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public EmptyRowsIterator()
    {
        Columns = [new Column("empty", DataType.Integer)];
        var frame = new RowsFrame(Columns);
        Current = new Row(frame);
        frame.AddRow(Current);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="schema">Target schema.</param>
    public EmptyRowsIterator(IRowsSchema schema)
    {
        Columns = schema.Columns;
        Current = new Row(schema);
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(false);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Empty");
    }
}
