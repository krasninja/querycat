using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// The exception occurs when required condition on rows input is omitted.
/// </summary>
[Serializable]
public sealed class QueryMissedCondition : QueryCatException
{
    public QueryMissedCondition(string columnName, IEnumerable<VariantValue.Operation> operations)
        : base(string.Format(Resources.Errors.QueryMissedRequiredCondition, columnName, string.Join(", ", operations)))
    {
    }

    private QueryMissedCondition(
        System.Runtime.Serialization.SerializationInfo serializationInfo,
        System.Runtime.Serialization.StreamingContext streamingContext)
    {
    }
}
