using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Rows input with the single value.
/// </summary>
internal sealed class SingleValueRowsInput : IRowsInput
{
    private readonly int _id = IdGenerator.GetNext();

    private bool _wasRead;
    private readonly VariantValue _value = VariantValue.Null;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public string[] UniqueKey { get; } = [];

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    public SingleValueRowsInput()
    {
        Columns = new Column[]
        {
            new(SingleValueRowsIterator.ColumnTitle, DataType.Integer)
        };
    }

    public SingleValueRowsInput(VariantValue value)
    {
        Columns =
        [
            new(SingleValueRowsIterator.ColumnTitle, value.Type)
        ];
        _value = value;
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = _value;
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (_wasRead)
        {
            return ValueTask.FromResult(false);
        }
        _wasRead = true;
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _wasRead = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Single value input (value={_value}, id={_id})");
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name} (value={_value}, id={_id})";
}
