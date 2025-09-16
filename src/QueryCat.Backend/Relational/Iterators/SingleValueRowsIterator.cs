using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational.Iterators;

public sealed class SingleValueRowsIterator : IRowsIterator
{
    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current { get; }

    private bool _isIterated;

    public SingleValueRowsIterator()
    {
        Columns =
        [
            new Column(Column.ValueColumnTitle, DataType.Void)
        ];
        Current = new Row(this)
        {
            [0] = VariantValue.Null
        };
    }

    public SingleValueRowsIterator(VariantValue value)
    {
        Columns =
        [
            new Column(Column.ValueColumnTitle, value.Type)
        ];
        Current = new Row(this)
        {
            [0] = value
        };
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_isIterated)
        {
            return ValueTask.FromResult(false);
        }
        _isIterated = true;
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isIterated = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Single");
    }
}
