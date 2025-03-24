using System.Collections;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Commands.Select.KeyConditionValue;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal sealed class SelectQueryConditions : IEnumerable<SelectQueryCondition>
{
    private readonly List<SelectQueryCondition> _conditions = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(SelectQueryConditions));

    /// <summary>
    /// Get query conditions.
    /// </summary>
    public IReadOnlyList<SelectQueryCondition> Conditions => _conditions;

    /// <summary>
    /// Get key conditions.
    /// </summary>
    /// <returns>Key condition.</returns>
    internal IReadOnlyList<SelectQueryCondition> GetKeyConditions(IRowsInputKeys rowsInputKeys)
    {
        var listConditions = new List<SelectQueryCondition>(capacity: Conditions.Count);
        foreach (var condition in Conditions)
        {
            if (rowsInputKeys.FindKeyColumn(condition.Column, condition.Operation) != null)
            {
                listConditions.Add(condition);
            }
        }
        return listConditions;
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
    /// <param name="generator">Values generator strategy.</param>
    internal SelectQueryCondition? TryAddCondition(
        Column column,
        VariantValue.Operation operation,
        IKeyConditionSingleValueGenerator generator)
    {
        if (_conditions.Any(c => c.Column == column && c.Operation == operation))
        {
            return null;
        }
        var queryContextCondition = new SelectQueryCondition(column, operation, generator);
        _conditions.Add(queryContextCondition);
        return queryContextCondition;
    }

    internal IEnumerable<SelectInputKeysConditions> GetConditionsColumns(IRowsSource input, string? alias = null)
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

            // The special condition for equals check. For example, input contains key column "date" with ">=" and "<=" conditions.
            // But the query is called like "date = now()". Instead of fail we convert it into "date >= now() AND date <= now()".
            if (matchConditions.Length == 0)
            {
                var equalsCondition = relatedConditions.FirstOrDefault(c => c.Operation == VariantValue.Operation.Equals);
                if (equalsCondition != null
                    && (keyColumn.ContainsOperation(VariantValue.Operation.GreaterOrEquals) || keyColumn.ContainsOperation(VariantValue.Operation.LessOrEquals)))
                {
                    matchConditions =
                    [
                        new(column, VariantValue.Operation.GreaterOrEquals, equalsCondition.Generator),
                        new(column, VariantValue.Operation.LessOrEquals, equalsCondition.Generator),
                    ];
                }
            }

            // The special condition for equals check. For example, input contains key column "id" with equals condition.
            // But the query is called like "id in (1, 2, 3)". Instead, we should "rewrite" the query to call it 3 times with
            // keys 1, 2 and 3.
            if (matchConditions.Length == 0)
            {
                var inCondition = relatedConditions.FirstOrDefault(c => c.Operation == VariantValue.Operation.In);
                if (inCondition != null && keyColumn.ContainsOperation(VariantValue.Operation.Equals))
                {
                    matchConditions =
                    [
                        new(column, VariantValue.Operation.Equals, inCondition.Generator),
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
