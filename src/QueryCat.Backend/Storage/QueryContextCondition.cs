using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

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

    #region Serialization

    internal string Serialize()
        => $"{Column.Name},{(int)Operation},{string.Join(",", Values.Select(DataTypeUtils.SerializeVariantValue))}";

    /// <summary>
    /// Deserialize from string. The string format is "columnName,operationIndex,value1,value2".
    /// </summary>
    /// <param name="columnFinder">Delegate to find column by name.</param>
    /// <param name="str">Target string.</param>
    internal static QueryContextCondition? CreateFromString(Func<string, Column?> columnFinder, string str)
    {
        var arr = StringUtils.GetFieldsFromLine(str);
        if (arr.Length < 3)
        {
            return null;
        }

        var column = columnFinder.Invoke(arr[0]);
        if (column == null)
        {
            return null;
        }

        return new QueryContextCondition(
            column,
            (VariantValue.Operation)int.Parse(arr[1]),
            arr[2..].Select(v => DataTypeUtils.DeserializeVariantValue(v)).ToArray());
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() => $"Column = {Column}, Operation = {Operation}";
}
