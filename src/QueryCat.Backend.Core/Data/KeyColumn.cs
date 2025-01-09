using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Key column.
/// </summary>
public sealed class KeyColumn
{
    /// <summary>
    /// Target source column index.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// The column is required to process the query, the error occurs otherwise.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// The first operation under which the key can be applied.
    /// </summary>
    public VariantValue.Operation Operation1 { get; }

    /// <summary>
    /// The second alternative operation under which the key can be applied.
    /// </summary>
    public VariantValue.Operation? Operation2 { get; }

    public KeyColumn(
        int columnIndex,
        bool isRequired = false,
        VariantValue.Operation operation1 = VariantValue.Operation.Equals,
        VariantValue.Operation? operation2 = null)
    {
        ColumnIndex = columnIndex;
        IsRequired = isRequired;
        Operation1 = operation1;
        Operation2 = operation2;
    }

    public KeyColumn(
        int columnIndex,
        bool isRequired,
        VariantValue.Operation[] operations) : this(
            columnIndex,
            isRequired,
            operations.Length > 0 ? operations[0] : VariantValue.Operation.Equals,
            operations.Length > 1 ? operations[1] : null)
    {
    }

    public IEnumerable<VariantValue.Operation> GetOperations()
    {
        yield return Operation1;
        if (Operation2 != null)
        {
            yield return Operation2.Value;
        }
    }

    public bool ContainsOperation(VariantValue.Operation operation)
        => Operation1 == operation || (Operation2.HasValue && Operation2.Value == operation);

    /// <inheritdoc />
    public override string ToString() => $"{(IsRequired ? "* " : "")} {ColumnIndex} ({Operation1}, {Operation2})";
}
