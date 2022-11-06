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
    /// Returns <c>true</c> if we can find key column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <param name="value">Condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool HasKeyCondition(string columnName, VariantValue.Operation operation,
        VariantValue.Operation orOperation, out VariantValue value)
    {
        if (!InputInfo.KeyColumns.Any(k => Column.NameEquals(k.ColumnName, columnName)
                && k.Operations.Contains(operation)
                && k.Operations.Contains(orOperation)))
        {
            value = VariantValue.Null;
            return false;
        }
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
    /// Returns <c>true</c> if we can find key column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="value">Condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool HasKeyCondition(string columnName, VariantValue.Operation operation, out VariantValue value)
        => HasKeyCondition(columnName, operation, operation, out value);

    internal IEnumerable<QueryContextCondition> GetKeyConditions()
    {
        foreach (var condition in QueryInfo.Conditions)
        {
            if (HasKeyCondition(condition.Column.Name, condition.Operation, out _))
            {
                yield return condition;
            }
        }
    }
}
