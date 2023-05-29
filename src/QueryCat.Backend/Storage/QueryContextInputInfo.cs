using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Query context input info.
/// </summary>
public sealed class QueryContextInputInfo
{
    public record KeyColumn(string ColumnName, VariantValue.Operation[] Operations)
    {
        public bool IsRequired { get; set; }

        public Action<VariantValue>? Action { get; set; }
    }

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

    #region Key columns

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <param name="action">Action to be executed before get data.</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo AddKeyColumn(
        string columnName,
        bool isRequired = false,
        Action<VariantValue>? action = null)
    {
        _keyColumns.Add(new KeyColumn(columnName, new[] { VariantValue.Operation.Equals })
        {
            IsRequired = isRequired,
            Action = action,
        });
        return this;
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Key operation.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <param name="action">Action to be executed before get data.</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo AddKeyColumn(
        string columnName,
        VariantValue.Operation operation,
        bool isRequired = false,
        Action<VariantValue>? action = null)
    {
        _keyColumns.Add(new KeyColumn(columnName, new[] { operation })
        {
            IsRequired = isRequired,
            Action = action,
        });
        return this;
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Key operation.</param>
    /// <param name="orOperation">Alternate key operation.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <param name="action">Action to be executed before get data.</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo AddKeyColumn(
        string columnName,
        VariantValue.Operation operation,
        VariantValue.Operation orOperation,
        bool isRequired = false,
        Action<VariantValue>? action = null)
    {
        _keyColumns.Add(new KeyColumn(columnName, new[] { operation, orOperation })
        {
            IsRequired = isRequired,
            Action = action,
        });
        return this;
    }

    /// <summary>
    /// Find key column.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <returns>Key column or null if not found.</returns>
    internal KeyColumn? FindKeyColumn(string columnName,
        VariantValue.Operation operation,
        VariantValue.Operation? orOperation = null)
    {
        return KeyColumns.FirstOrDefault(k => Column.NameEquals(k.ColumnName, columnName)
            && k.Operations.Contains(operation)
            && (!orOperation.HasValue || k.Operations.Contains(orOperation.Value)));
    }

    #endregion

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

    /// <summary>
    /// Merge rows input arguments to distinct it among other queries of the same input.
    /// </summary>
    /// <param name="arguments">Input arguments (file name, ids).</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo MergeInputArguments(params string[] arguments)
    {
        if (arguments.Length > 0)
        {
            SetInputArguments(InputArguments.Concat(arguments).ToArray());
        }
        return this;
    }
}
