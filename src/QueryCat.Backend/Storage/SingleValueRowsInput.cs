using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Rows input with the single value.
/// </summary>
internal sealed class SingleValueRowsInput : IRowsInput
{
    private bool _wasRead;
    private readonly VariantValue _value = VariantValue.Null;

    /// <inheritdoc />
    public Column[] Columns { get; }

    public SingleValueRowsInput()
    {
        Columns = new Column[]
        {
            new(SingleValueRowsIterator.ColumnTitle, DataType.Integer)
        };
    }

    public SingleValueRowsInput(VariantValue value)
    {
        Columns = new Column[]
        {
            new(SingleValueRowsIterator.ColumnTitle, value.GetInternalType())
        };
        _value = value;
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = _value;
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        if (_wasRead)
        {
            return false;
        }
        _wasRead = true;
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _wasRead = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Single value input {_value}");
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name} (value={_value})";
}
