using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Context for input rows source.
/// </summary>
public class SelectInputQueryContext : QueryContext
{
    internal record struct KeyColumn(string ColumnName, VariantValue.Operation[] Operations);

    private readonly List<KeyColumn> _keyColumns = new();

    private readonly List<string> _inputArguments = new();

    /// <summary>
    /// The target rows input.
    /// </summary>
    public IRowsInput RowsInput { get; }

    internal List<QueryContextCondition> Conditions { get; } = new();

    /// <summary>
    /// Limit rows count.
    /// </summary>
    internal long? Limit { get; set; }

    /// <inheritdoc />
    public SelectInputQueryContext(IRowsInput rowsInput)
    {
        RowsInput = rowsInput;
    }

    /// <inheritdoc />
    public override IReadOnlyList<Column> GetColumns() => RowsInput.Columns;

    /// <inheritdoc />
    public override IReadOnlyList<QueryContextCondition> GetConditions() => Conditions;

    /// <inheritdoc />
    public override long? GetLimit() => Limit;

    internal IReadOnlyList<string> GetInputArguments() => _inputArguments;

    /// <inheritdoc />
    public override QueryContext AddInputArguments(params string[] keys)
    {
        _inputArguments.AddRange(keys);
        return this;
    }

    /// <inheritdoc />
    public override QueryContext AddKeyColumn(string columnName, params VariantValue.Operation[] operations)
    {
        _keyColumns.Add(new KeyColumn(columnName, operations));
        return this;
    }

    internal IEnumerable<QueryContextCondition> GetKeyConditions()
    {
        foreach (var condition in GetConditions())
        {
            if (HasKeyCondition(condition.Column.Name, condition.Operation, out _))
            {
                yield return condition;
            }
        }
    }

    /// <inheritdoc />
    public override bool HasKeyCondition(string columnName, VariantValue.Operation operation, VariantValue.Operation orOperation, out VariantValue value)
    {
        if (!_keyColumns.Any(k => Column.NameEquals(k.ColumnName, columnName)
                && k.Operations.Contains(operation)
                && k.Operations.Contains(orOperation)))
        {
            value = VariantValue.Null;
            return false;
        }
        foreach (var condition in GetConditions())
        {
            if (Column.NameEquals(condition.Column, columnName)
                && (condition.Operation == operation || condition.Operation == orOperation))
            {
                value = condition.ValueFunc.Invoke();
                return true;
            }
        }

        value = VariantValue.Null;
        return false;
    }

    /// <inheritdoc />
    internal override CacheKey GetCacheKey()
        => new(
            RowsInput.GetType().Name,
            inputArguments: GetInputArguments().ToArray(),
            selectColumns: GetColumns().Select(c => c.Name).ToArray(),
            conditions: GetKeyConditions().ToArray(),
            offset: 0,
            limit: GetLimit());
}
