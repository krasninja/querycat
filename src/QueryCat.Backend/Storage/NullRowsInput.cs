using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The rows input that does nothing.
/// </summary>
public sealed class NullRowsInput : IRowsInput
{
    public static NullRowsInput Instance { get; } = new();

    /// <inheritdoc />
    public Column[] Columns => Array.Empty<Column>();

    /// <inheritdoc />
    public string[] UniqueKey { get; } = Array.Empty<string>();

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = EmptyQueryContext.Empty;

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = VariantValue.Null;
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Null");
    }
}
