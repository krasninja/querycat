using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class contains information about query - selected columns, conditions.
/// </summary>
public sealed class QueryContextQueryInfo
{
    /// <summary>
    /// Get columns for select.
    /// </summary>
    public IReadOnlyList<Column> Columns { get; internal set; }

    private readonly List<QueryContextCondition> _queryContextConditions = new();

    /// <summary>
    /// Get query conditions.
    /// </summary>
    public IReadOnlyList<QueryContextCondition> Conditions => _queryContextConditions;

    /// <summary>
    /// Rows offset.
    /// </summary>
    public long Offset { get; internal set; }

    /// <summary>
    /// Get rows limit. Allows to select only needed amount of records.
    /// </summary>
    public long? Limit { get; internal set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Select columns.</param>
    /// <param name="limit">Select limit.</param>
    public QueryContextQueryInfo(IReadOnlyList<Column> columns, long? limit = null)
    {
        Columns = columns;
        Limit = limit;
    }

    /// <summary>
    /// Get key conditions.
    /// </summary>
    /// <returns>Key condition.</returns>
    internal IEnumerable<QueryContextCondition> GetKeyConditions(IRowsInputKeys rowsInputKeys)
    {
        foreach (var condition in Conditions)
        {
            if (rowsInputKeys.FindKeyColumn(condition.Column.Name, condition.Operation) != null)
            {
                yield return condition;
            }
        }
    }

    /// <summary>
    /// Add condition.
    /// </summary>
    /// <param name="condition">Condition instance.</param>
    internal void AddCondition(QueryContextCondition condition)
    {
        _queryContextConditions.Add(condition);
    }

    /// <summary>
    /// Add condition.
    /// </summary>
    /// <param name="column">Condition column.</param>
    /// <param name="operation">Condition operation.</param>
    /// <param name="valueFunctions">Value functions.</param>
    internal QueryContextCondition AddCondition(
        Column column,
        VariantValue.Operation operation,
        params IFuncUnit[] valueFunctions)
    {
        var queryContextCondition = new QueryContextCondition(column, operation, valueFunctions);
        _queryContextConditions.Add(queryContextCondition);
        return queryContextCondition;
    }

    #region Get condition value

    /// <summary>
    /// Returns <c>true</c> if we can find key column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <param name="value">Condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool TryGetConditionValue(string columnName, VariantValue.Operation operation,
        VariantValue.Operation orOperation, out VariantValue value)
    {
        foreach (var condition in Conditions)
        {
            if (Column.NameEquals(condition.Column, columnName)
                && (condition.Operation == operation || condition.Operation == orOperation))
            {
                value = condition.ValueFunc.Invoke();
                return true;
            }
        }

        value = VariantValue.Null;
        return false;
    }

    /// <summary>
    /// Get required column condition or throw exception.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <returns>Variant value.</returns>
    public VariantValue GetConditionValue(string columnName, VariantValue.Operation operation,
        VariantValue.Operation orOperation)
    {
        if (TryGetConditionValue(columnName, operation, orOperation, out VariantValue value))
        {
            return value;
        }
        else
        {
            var operations = string.Join(", ", new[] { operation, orOperation }.Distinct());
            throw new QueryCatException($"The input do not have {operations} condition(-s) on column {columnName}.");
        }
    }

    /// <summary>
    /// Returns <c>true</c> if we can find key column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="value">Condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool TryGetConditionValue(string columnName, VariantValue.Operation operation, out VariantValue value)
        => TryGetConditionValue(columnName, operation, operation, out value);

    /// <summary>
    /// Get required column condition or throw exception.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <returns>Variant value.</returns>
    public VariantValue GetConditionValue(string columnName, VariantValue.Operation operation)
        => GetConditionValue(columnName, operation, operation);

    /// <summary>
    /// Returns <c>true</c> if we can find key column equal condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="value">Equal condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool TryGetConditionValue(string columnName, out VariantValue value)
        => TryGetConditionValue(columnName, VariantValue.Operation.Equals, VariantValue.Operation.Equals, out value);

    /// <summary>
    /// Get required column condition or throw exception.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <returns>Variant value.</returns>
    public VariantValue GetConditionValue(string columnName)
        => GetConditionValue(columnName, VariantValue.Operation.Equals, VariantValue.Operation.Equals);

    #endregion
}
