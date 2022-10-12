using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Provides helpful methods to process query context conditions.
/// </summary>
public class QueryContextProcessor
{
    private record struct ConditionAction(
        string ColumnName,
        VariantValue.Operation[] Operations,
        Action<QueryContextCondition> Action);

    private readonly List<ConditionAction> _conditionActions = new();

    /// <summary>
    /// Query context.
    /// </summary>
    public QueryContext QueryContext { get; }

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
    /// Run processing.
    /// </summary>
    public void Run()
    {
        foreach (var condition in QueryContext.GetConditions())
        {
            foreach (var conditionAction in _conditionActions)
            {
                if (Column.NameEquals(condition.Column, conditionAction.ColumnName)
                    && conditionAction.Operations.Contains(condition.Operation))
                {
                    conditionAction.Action.Invoke(condition);
                    break;
                }
            }
        }
    }
}
