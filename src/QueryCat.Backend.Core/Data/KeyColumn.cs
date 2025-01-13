using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Key column.
/// </summary>
public sealed class KeyColumn
{
    private readonly VariantValue.Operation[] _operations;

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
    public VariantValue.Operation Operation1 => _operations[0];

    /// <summary>
    /// The second alternative operation under which the key can be applied.
    /// </summary>
    public VariantValue.Operation? Operation2 => _operations.Length > 1 ? _operations[1] : null;

    public KeyColumn(
        int columnIndex,
        bool isRequired = false,
        VariantValue.Operation operation1 = VariantValue.Operation.Equals,
        VariantValue.Operation? operation2 = null)
    {
        ColumnIndex = columnIndex;
        IsRequired = isRequired;
        _operations = operation2.HasValue ? [operation1, operation2.Value] : [operation1];
    }

    public KeyColumn(
        int columnIndex,
        bool isRequired,
        VariantValue.Operation[] operations) : this(columnIndex, isRequired)
    {
        _operations = operations;
    }

    /// <summary>
    /// Get all available operations.
    /// </summary>
    /// <returns>Operations.</returns>
    public IEnumerable<VariantValue.Operation> GetOperations() => _operations;

    /// <summary>
    /// Returns <c>true</c> if key column contains the operation.
    /// </summary>
    /// <param name="operation">Operation to check.</param>
    /// <returns><c>True</c> if contains, <c>false</c> otherwise.</returns>
    public bool ContainsOperation(VariantValue.Operation operation) => _operations.Contains(operation);

    /// <inheritdoc />
    public override string ToString()
        => $"{(IsRequired ? "* " : "")} {ColumnIndex} ({string.Join(", ", _operations)})";
}
