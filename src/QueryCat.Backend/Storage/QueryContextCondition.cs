using QueryCat.Backend.Functions;
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

    public IReadOnlyList<IFuncUnit> ValueFuncs { get; }

    public IFuncUnit ValueFunc => ValueFuncs[0];

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="column">Filter column.</param>
    /// <param name="operation">Filter operation.</param>
    /// <param name="valueFuncs">Filter values.</param>
    public QueryContextCondition(
        Column column,
        VariantValue.Operation operation,
        IReadOnlyList<IFuncUnit> valueFuncs)
    {
        if (valueFuncs.Count < 1)
        {
            throw new ArgumentException("No values.", nameof(valueFuncs));
        }

        this.Column = column;
        this.Operation = operation;
        this.ValueFuncs = valueFuncs;
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
        IFuncUnit value) : this(column, operation, new[] { value })
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"Column = {Column}, Operation = {Operation}";
}
