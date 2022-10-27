using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Provides helpful methods to process query context conditions.
/// </summary>
public class QueryContextProcessor
{
    private record ConditionAction(
        string ColumnName,
        VariantValue.Operation[] Operations,
        Action<QueryContextCondition> Action,
        bool IsRequired = false);

    private readonly List<ConditionAction> _conditionActions = new();

    /// <summary>
    /// Query context.
    /// </summary>
    public QueryContext QueryContext { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="queryContext">Query context.</param>
    public QueryContextProcessor(QueryContext queryContext)
    {
        QueryContext = queryContext;
    }

    /// <summary>
    /// Register equals condition.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="action">Action.</param>
    /// <returns>Instance of <see cref="QueryContextProcessor" />.</returns>
    public QueryContextProcessor RegisterCondition(string name, Action<QueryContextCondition> action)
    {
        _conditionActions.Add(new ConditionAction(name, new[] { VariantValue.Operation.Equals }, action));
        return this;
    }

    /// <summary>
    /// Register required equals condition.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="action">Action.</param>
    /// <returns>Instance of <see cref="QueryContextProcessor" />.</returns>
    public QueryContextProcessor RegisterRequiredCondition(string name, Action<QueryContextCondition> action)
    {
        _conditionActions.Add(new ConditionAction(name, new[] { VariantValue.Operation.Equals }, action, true));
        return this;
    }

    /// <summary>
    /// Register condition.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="action">Action.</param>
    /// <returns>Instance of <see cref="QueryContextProcessor" />.</returns>
    public QueryContextProcessor RegisterCondition(string name, VariantValue.Operation operation, Action<QueryContextCondition> action)
    {
        _conditionActions.Add(new ConditionAction(name, new[] { operation }, action));
        return this;
    }

    /// <summary>
    /// Register required condition.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="action">Action.</param>
    /// <returns>Instance of <see cref="QueryContextProcessor" />.</returns>
    public QueryContextProcessor RegisterRequiredCondition(string name, VariantValue.Operation operation, Action<QueryContextCondition> action)
    {
        _conditionActions.Add(new ConditionAction(name, new[] { operation }, action, true));
        return this;
    }

    /// <summary>
    /// Register condition.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="operation1">Operation 1.</param>
    /// <param name="operation2">Operation 2.</param>
    /// <param name="action">Action.</param>
    /// <returns>Instance of <see cref="QueryContextProcessor" />.</returns>
    public QueryContextProcessor RegisterCondition(string name, VariantValue.Operation operation1,
        VariantValue.Operation operation2, Action<QueryContextCondition> action)
    {
        _conditionActions.Add(new ConditionAction(name, new[] { operation1, operation2 }, action));
        return this;
    }

    /// <summary>
    /// Register required condition.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="operation1">Operation 1.</param>
    /// <param name="operation2">Operation 2.</param>
    /// <param name="action">Action.</param>
    /// <returns>Instance of <see cref="QueryContextProcessor" />.</returns>
    public QueryContextProcessor RegisterRequiredCondition(string name, VariantValue.Operation operation1,
        VariantValue.Operation operation2, Action<QueryContextCondition> action)
    {
        _conditionActions.Add(new ConditionAction(name, new[] { operation1, operation2 }, action, true));
        return this;
    }

    /// <summary>
    /// Run processing.
    /// </summary>
    /// <returns>Applied count.</returns>
    public int Run()
    {
        void LogProcessed(IEnumerable<ConditionAction> processed)
        {
            foreach (var conditionAction in processed)
            {
                var operations = string.Join(", ", conditionAction.Operations.Select(o => $"'{o}'"));
                Logger.Instance.Trace(
                    $"Applied condition for column '{conditionAction.ColumnName}' and operations {operations}.",
                    nameof(QueryContextProcessor));
            }
        }

        var conditions = QueryContext.GetConditions();

        var processed = ApplyConditions(conditions);
        processed = processed.Union(ApplyGreaterLessOperationInsteadOfEquals(conditions));

        processed = processed.ToList();
        LogProcessed(processed);

        var requiredNotProcessed = _conditionActions.Where(ca => ca.IsRequired).Except(processed);
        var requiredNotProcessedFirst = requiredNotProcessed.FirstOrDefault();
        if (requiredNotProcessedFirst != null)
        {
            var operations = string.Join(", ", requiredNotProcessedFirst.Operations.Select(o => $"'{o}'"));
            throw new QueryCatException(
                $"The input requires filter by '{requiredNotProcessedFirst.ColumnName}' with operations {operations}.");
        }

        return processed.Count();
    }

    private IEnumerable<ConditionAction> ApplyConditions(IReadOnlyList<QueryContextCondition> conditions)
    {
        var processed = new List<ConditionAction>();
        foreach (var condition in conditions)
        {
            foreach (var conditionAction in _conditionActions)
            {
                if (Column.NameEquals(condition.Column, conditionAction.ColumnName)
                    && conditionAction.Operations.Contains(condition.Operation))
                {
                    processed.Add(conditionAction);
                    conditionAction.Action.Invoke(condition);
                    break;
                }
            }
        }
        return processed;
    }

    /// <summary>
    /// If we have "greater or equal" and "less or equal" required operations, but user provided only "equal" operation,
    /// we change it.
    /// </summary>
    private IEnumerable<ConditionAction> ApplyGreaterLessOperationInsteadOfEquals(IReadOnlyList<QueryContextCondition> conditions)
    {
        return new ConditionAction[] { };
    }
}
