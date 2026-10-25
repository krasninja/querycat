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
        Unknown = -1,
        Function = 0,
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
    public string InputSourceName { get; }

    /// <summary>
    /// Input source type.
    /// </summary>
    public MissedConditionInputSourceType InputSourceType { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sourceName">Source name.</param>
    /// <param name="sourceType">Source type.</param>
    /// <param name="columnName">Column name.</param>
    /// <param name="operations">Operations.</param>
    /// <param name="innerException">Inner exception.</param>
    public QueryMissedConditionException(
        string sourceName,
        MissedConditionInputSourceType sourceType,
        string columnName,
        IReadOnlyCollection<VariantValue.Operation> operations,
        Exception? innerException)
        : base(
            string.Format(Resources.Errors.QueryMissedRequiredCondition, columnName, string.Join(", ", operations)),
            innerException)
    {
        ColumnName = columnName;
        Operations = operations;
        InputSourceName = sourceName;
        InputSourceType = sourceType;
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
        : this(sourceName, sourceType, columnName, operations, null)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operations">Operations.</param>
    public QueryMissedConditionException(
        string columnName,
        IReadOnlyCollection<VariantValue.Operation> operations)
        : this(string.Empty, MissedConditionInputSourceType.Unknown, columnName, operations, null)
    {
    }
}
