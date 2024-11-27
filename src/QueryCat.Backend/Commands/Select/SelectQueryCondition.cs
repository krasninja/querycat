using QueryCat.Backend.Commands.Select.KeyConditionValue;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
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

    public IKeyConditionSingleValueGenerator Generator { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="column">Filter column.</param>
    /// <param name="operation">Filter operation.</param>
    /// <param name="generator">Values generator strategy.</param>
    public SelectQueryCondition(
        Column column,
        VariantValue.Operation operation,
        IKeyConditionSingleValueGenerator generator)
    {
        this.Column = column;
        this.Operation = operation;
        this.Generator = generator;
    }

    public CacheKeyCondition ToCacheCondition(IExecutionThread thread)
        => new(Column, Operation, Generator.GetValues(thread));

    /// <inheritdoc />
    public override string ToString() => $"Column = {Column}, Operation = {Operation}";
}
