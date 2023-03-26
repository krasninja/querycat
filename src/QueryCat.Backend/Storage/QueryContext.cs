using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    /// <summary>
    /// Information about the rows input.
    /// </summary>
    public QueryContextInputInfo InputInfo { get; set; } = new(NullRowsInput.Instance);

    /// <summary>
    /// Information about the query where the rows input is used.
    /// </summary>
    public abstract QueryContextQueryInfo QueryInfo { get; }

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Input config storage.
    /// </summary>
    public IInputConfigStorage InputConfigStorage { get; internal set; } = new MemoryInputConfigStorage();

    public QueryContext(IExecutionThread executionThread)
    {
        ExecutionThread = executionThread;
    }

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
        foreach (var condition in QueryInfo.Conditions)
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

    /// <summary>
    /// Get key conditions.
    /// </summary>
    /// <returns>Key condition.</returns>
    internal IEnumerable<QueryContextCondition> GetKeyConditions()
    {
        foreach (var condition in QueryInfo.Conditions)
        {
            if (InputInfo.FindKeyColumn(condition.Column.Name, condition.Operation) != null)
            {
                yield return condition;
            }
        }
    }

    /// <summary>
    /// Validate key columns and execute property actions.
    /// </summary>
    internal void ValidateAndInvokeKeyConditions()
    {
        foreach (var keyColumn in InputInfo.KeyColumns)
        {
            var condition = QueryInfo.Conditions.FirstOrDefault(c =>
                Column.NameEquals(c.Column, keyColumn.ColumnName)
                    && keyColumn.Operations.Contains(c.Operation));
            if (condition == null && keyColumn.IsRequired)
            {
                throw new QueryContextMissedCondition(keyColumn.ColumnName, keyColumn.Operations);
            }
            if (keyColumn.Action != null && condition != null)
            {
                keyColumn.Action.Invoke(condition.ValueFunc.Invoke());
            }
        }
    }
}
