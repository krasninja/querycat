using System.Collections;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal sealed class SelectQueryConditions : IEnumerable<SelectQueryCondition>
{
    private readonly List<SelectQueryCondition> _conditions = new();

    /// <summary>
    /// Get query conditions.
    /// </summary>
    public IReadOnlyList<SelectQueryCondition> Conditions => _conditions;

    /// <summary>
    /// Get key conditions.
    /// </summary>
    /// <returns>Key condition.</returns>
    internal IEnumerable<SelectQueryCondition> GetKeyConditions(IRowsInputKeys rowsInputKeys)
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
    internal void AddCondition(SelectQueryCondition condition)
    {
        _conditions.Add(condition);
    }

    /// <summary>
    /// Add condition.
    /// </summary>
    /// <param name="column">Condition column.</param>
    /// <param name="operation">Condition operation.</param>
    /// <param name="valueFunctions">Value functions.</param>
    internal SelectQueryCondition AddCondition(
        Column column,
        VariantValue.Operation operation,
        params IFuncUnit[] valueFunctions)
    {
        var queryContextCondition = new SelectQueryCondition(column, operation, valueFunctions);
        _conditions.Add(queryContextCondition);
        return queryContextCondition;
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

    /// <inheritdoc />
    public IEnumerator<SelectQueryCondition> GetEnumerator()
    {
        foreach (var condition in _conditions)
        {
            yield return condition;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
