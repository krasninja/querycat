using System.Collections.Immutable;
using System.Text;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The cache key for the specific instance of <see cref="IRowsInput" />.
/// </summary>
internal readonly struct CacheKey
{
    /// <summary>
    /// Input function class/name.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Input function arguments.
    /// </summary>
    public ImmutableHashSet<string> InputArguments { get; }

    /// <summary>
    /// Columns selected from input.
    /// </summary>
    public ImmutableHashSet<string> SelectColumns { get; }

    /// <summary>
    /// Key conditions that were applied to the input.
    /// </summary>
    public ImmutableHashSet<CacheKeyCondition> Conditions { get; }

    /// <summary>
    /// Applied offset.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Applied result limit. -1 means no limit.
    /// </summary>
    public long Limit { get; }

    /// <summary>
    /// Empty instance.
    /// </summary>
    public static CacheKey Empty { get; } = new(
        from: "empty",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<SelectQueryCondition>());

    public CacheKey(IRowsInput rowsInput, QueryContext queryContext, SelectQueryConditions conditions) : this(
        from: rowsInput.GetType().Name,
        inputArguments: rowsInput.UniqueKey,
        selectColumns: queryContext.QueryInfo.Columns.Select(c => c.Name).ToArray(),
        conditions: rowsInput is IRowsInputKeys rowsInputKeys ? conditions.GetKeyConditions(rowsInputKeys).ToArray() : null,
        offset: queryContext.QueryInfo.Offset,
        limit: queryContext.QueryInfo.Limit)
    {
    }

    public CacheKey(
        string from,
        string[] inputArguments,
        string[] selectColumns,
        SelectQueryCondition[]? conditions = null,
        long offset = 0,
        long? limit = null) : this(
            from,
            inputArguments.Where(a => !string.IsNullOrEmpty(a)).ToArray(),
            selectColumns,
            (conditions ?? Array.Empty<SelectQueryCondition>()).Select(c =>
                new CacheKeyCondition(
                    c.Column,
                    c.Operation,
                    new VariantValueArray(c.ValueFuncs.Select(f => f.Invoke()))
                )).ToArray(),
            offset,
            limit)
    {
    }

    internal CacheKey(
        string from,
        string[] inputArguments,
        string[] selectColumns,
        CacheKeyCondition[]? conditions = null,
        long offset = 0,
        long? limit = null)
    {
        From = from;
        InputArguments = ImmutableHashSet.Create(inputArguments);
        SelectColumns = ImmutableHashSet.Create(selectColumns);
        Conditions = ImmutableHashSet.Create(conditions ?? Array.Empty<CacheKeyCondition>());
        Offset = offset;
        Limit = limit ?? -1;
    }

    /// <summary>
    /// The function is to analyze if the current cache key can be used to process the provided one.
    /// </summary>
    /// <param name="key">Other key.</param>
    /// <returns>Returns <c>true</c> if the current cache key is withing other key subset, <c>false</c> otherwise.</returns>
    public bool Match(CacheKey key)
    {
        if (From != key.From)
        {
            return false;
        }
        if (!InputArguments.IsSubsetOf(key.InputArguments))
        {
            return false;
        }
        if (!SelectColumns.IsSubsetOf(key.SelectColumns))
        {
            return false;
        }
        if (!Conditions.IsSubsetOf(key.Conditions))
        {
            return false;
        }
        if (Offset < key.Offset)
        {
            return false;
        }
        if (Limit > key.Limit)
        {
            return false;
        }
        return true;
    }

    #region Serialization

    internal string Serialize()
    {
        var sb = new StringBuilder(32);
        sb.Append(StringUtils.Quote($"F:{From}"));
        if (InputArguments.Count > 0)
        {
            sb.Append(' ');
            var inputKeys = InputArguments.Select(ik => StringUtils.Quote("I:" + ik).ToString());
            sb.AppendJoin(' ', inputKeys);
        }
        if (SelectColumns.Count > 0)
        {
            sb.Append(' ');
            var columns = SelectColumns.Select(c => StringUtils.Quote("S:" + c).ToString());
            sb.AppendJoin(' ', columns);
        }
        if (Offset > 0)
        {
            sb.Append($" O:{Offset}");
        }
        if (Limit > 0)
        {
            sb.Append($" L:{Limit}");
        }
        if (Conditions.Count > 0)
        {
            sb.Append(' ');
            var conditions = Conditions.Select(c => StringUtils.Quote("W:" + c.Serialize()).ToString());
            sb.AppendJoin(' ', conditions);
        }
        return sb.ToString();
    }

    internal static CacheKey Deserialize(string str, params Column[] columns)
    {
        Column? FindColumnByName(string columnName) => columns.FirstOrDefault(c => c.Name.Equals(columnName));

        var arr = StringUtils.GetFieldsFromLine(str, delimiter: ' ');
        var from = string.Empty;
        var offset = 0;
        var limit = -1;
        List<string> selectColumns = new();
        List<string> inputKeys = new();
        List<CacheKeyCondition> conditions = new();
        foreach (var item in arr)
        {
            var key = item[..2];
            var value = StringUtils.Unquote(item.AsSpan(2)).ToString();
            if (key == "F:")
            {
                from = value;
            }
            else if (key == "I:")
            {
                inputKeys.Add(value);
            }
            else if (key == "S:")
            {
                selectColumns.Add(value);
            }
            else if (key == "W:")
            {
                if (CacheKeyCondition.Deserialize(FindColumnByName, value, out var condition))
                {
                    conditions.Add(condition);
                }
            }
            else if (key == "O:")
            {
                offset = int.Parse(value);
            }
            else if (key == "L:")
            {
                limit = int.Parse(value);
            }
        }
        return new CacheKey(from, inputKeys.ToArray(), selectColumns.ToArray(), conditions.ToArray(), offset, limit);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() => Serialize();
}
