using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Query filter condition.
/// </summary>
public class QueryContextCondition
{
    public Column Column { get; }

    public VariantValue.Operation Operation { get; }

    public IReadOnlyList<VariantValue> Values { get; }

    public VariantValue Value => Values[0];

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="column">Filter column.</param>
    /// <param name="operation">Filter operation.</param>
    /// <param name="values">Filter values.</param>
    public QueryContextCondition(
        Column column,
        VariantValue.Operation operation,
        IReadOnlyList<VariantValue> values)
    {
        if (values.Count < 1)
        {
            throw new ArgumentException(Resources.Errors.NoValues, nameof(values));
        }

        this.Column = column;
        this.Operation = operation;
        this.Values = values;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="column">Filter column.</param>
    /// <param name="operation">Filter operation.</param>
    /// <param name="value">Filter value.</param>
    public QueryContextCondition(
        Column column,
        VariantValue.Operation operation,
        VariantValue value) : this(column, operation, new[] { value })
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"Column = {Column}, Operation = {Operation}";
}
