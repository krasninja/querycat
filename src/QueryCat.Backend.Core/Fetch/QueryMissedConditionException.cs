using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// The exception occurs when required condition on rows input is omitted.
/// </summary>
public sealed class QueryMissedConditionException : QueryCatException
{
    public QueryMissedConditionException(string columnName, IEnumerable<VariantValue.Operation> operations)
        : base(string.Format(Resources.Errors.QueryMissedRequiredCondition, columnName, string.Join(", ", operations)))
    {
    }
}
