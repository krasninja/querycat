using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The exception occurs when required condition on rows input is omitted.
/// </summary>
[Serializable]
public sealed class QueryContextMissedCondition : QueryCatException
{
    public QueryContextMissedCondition(string columnName, IEnumerable<VariantValue.Operation> operations)
        : base($"Cannot find required condition '{columnName}' with condition(-s) {string.Join(", ", operations)}.")
    {
    }

    private QueryContextMissedCondition(
        System.Runtime.Serialization.SerializationInfo serializationInfo,
        System.Runtime.Serialization.StreamingContext streamingContext)
    {
    }
}
