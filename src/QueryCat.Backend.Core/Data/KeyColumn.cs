using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Key column.
/// </summary>
public sealed class KeyColumn
{
    public string ColumnName { get; }

    public bool IsRequired { get; }

    /// <summary>
    /// The operations under which the key can be applied.
    /// </summary>
    public VariantValue.Operation[] Operations { get; }

    public KeyColumn(string columnName, bool isRequired = false, params VariantValue.Operation[] operations)
    {
        ColumnName = columnName;
        IsRequired = isRequired;
        Operations = operations;
    }

    /// <inheritdoc />
    public override string ToString() => $"{(IsRequired ? "* " : "")} {ColumnName} ({string.Join(", ", Operations)})";
}