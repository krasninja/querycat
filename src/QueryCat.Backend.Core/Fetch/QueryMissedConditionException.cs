using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// The exception occurs when required condition on rows input is omitted.
/// </summary>
public sealed class QueryMissedConditionException : QueryCatException
{
    /// <summary>
    /// Input source type.
    /// </summary>
    public enum MissedConditionInputSourceType
    {
        Function,
        Identifier,
    }

    /// <summary>
    /// Required column name.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Required operations.
    /// </summary>
    public IReadOnlyCollection<VariantValue.Operation> Operations { get; }

    /// <summary>
    /// Input source name of function or identifier.
    /// </summary>
    public string InputSourceName { get; } = string.Empty;

    /// <summary>
    /// Input source type.
    /// </summary>
    public MissedConditionInputSourceType InputSourceType { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operations">Operations.</param>
    public QueryMissedConditionException(string columnName, IReadOnlyCollection<VariantValue.Operation> operations)
        : base(string.Format(Resources.Errors.QueryMissedRequiredCondition, columnName, string.Join(", ", operations)))
    {
        ColumnName = columnName;
        Operations = operations;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sourceName">Source name.</param>
    /// <param name="sourceType">Source type.</param>
    /// <param name="columnName">Column name.</param>
    /// <param name="operations">Operations.</param>
    public QueryMissedConditionException(
        string sourceName,
        MissedConditionInputSourceType sourceType,
        string columnName,
        IReadOnlyCollection<VariantValue.Operation> operations)
        : this(columnName, operations)
    {
        InputSourceName = sourceName;
        InputSourceType = sourceType;
    }
}
