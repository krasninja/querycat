using System.Collections;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
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
            if (rowsInputKeys.FindKeyColumn(condition.Column, condition.Operation) != null)
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

    internal IEnumerable<SelectInputKeysConditions> GetConditionsColumns(IRowsInput input, string? alias = null)
    {
        if (input is not IRowsInputKeys inputKey)
        {
            yield break;
        }

        foreach (var keyColumn in inputKey.GetKeyColumns())
        {
            var column = inputKey.Columns[keyColumn.ColumnIndex];
            var relatedConditions = Conditions
                .Where(c =>
                    c.Column == column
                    && (alias == null || column.SourceName == alias))
                .ToArray();

            // Straight conditions check.
            var matchConditions = relatedConditions.Where(c => keyColumn.ContainsOperation(c.Operation)).ToArray();

            // The special condition for equals check. For example, input contains key column date with ">=" and "<=" conditions.
            // But the query is called like "date = now()". Instead of fail we convert it into "date >= now() AND date <= now()".
            if (matchConditions.Length == 0)
            {
                var equalsCondition = relatedConditions.FirstOrDefault(c => c.Operation == VariantValue.Operation.Equals);
                if (equalsCondition != null
                    && (keyColumn.ContainsOperation(VariantValue.Operation.GreaterOrEquals) || keyColumn.ContainsOperation(VariantValue.Operation.LessOrEquals)))
                {
                    matchConditions =
                    [
                        new(column, VariantValue.Operation.GreaterOrEquals, equalsCondition.ValueFuncs),
                        new(column, VariantValue.Operation.LessOrEquals, equalsCondition.ValueFuncs),
                    ];
                }
            }

            yield return new SelectInputKeysConditions(inputKey, column, keyColumn, matchConditions);
        }
    }

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
