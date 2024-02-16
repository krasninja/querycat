using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Query filter condition.
/// </summary>
internal sealed class SelectQueryCondition
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
    public SelectQueryCondition(
        Column column,
        VariantValue.Operation operation,
        IReadOnlyList<IFuncUnit> valueFuncs)
    {
        if (valueFuncs.Count < 1)
        {
            throw new ArgumentException(Resources.Errors.NoValues, nameof(valueFuncs));
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
    public SelectQueryCondition(
        Column column,
        VariantValue.Operation operation,
        IFuncUnit value) : this(column, operation, new[] { value })
    {
    }

    public CacheKeyCondition ToCacheCondition()
        => new(Column, Operation, ValueFuncs.Select(f => f.Invoke()).ToArray());

    /// <inheritdoc />
    public override string ToString() => $"Column = {Column}, Operation = {Operation}";
}
