using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Query context input info.
/// </summary>
public sealed class QueryContextInputInfo
{
    public record struct KeyColumn(string ColumnName, VariantValue.Operation[] Operations);

    private readonly List<KeyColumn> _keyColumns = new();

    /// <summary>
    /// The key columns. These rows input columns allows optimize fetch.
    /// </summary>
    public IReadOnlyList<KeyColumn> KeyColumns => _keyColumns;

    /// <summary>
    /// Specific rows input.
    /// </summary>
    public IRowsInput RowsInput { get; }

    /// <summary>
    /// Rows input identifier (class name, function name, etc).
    /// </summary>
    public string RowsInputId { get; }

    /// <summary>
    /// Input arguments.
    /// </summary>
    public string[] InputArguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsInput">Rows input.</param>
    public QueryContextInputInfo(IRowsInput rowsInput)
    {
        RowsInput = rowsInput;
        RowsInputId = rowsInput.GetType().Name;
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operations">Key operations.</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo AddKeyColumn(string columnName, params VariantValue.Operation[] operations)
    {
        _keyColumns.Add(new KeyColumn(columnName, operations));
        return this;
    }

    /// <summary>
    /// Set rows input arguments to distinct it among other queries of the same input.
    /// </summary>
    /// <param name="arguments">Input arguments (file name, ids).</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo SetInputArguments(params string[] arguments)
    {
        InputArguments = arguments;
        return this;
    }
}
