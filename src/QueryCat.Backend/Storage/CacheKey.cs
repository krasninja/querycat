using System.Text;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The cache key for the specific instance of <see cref="IRowsInput" />.
/// </summary>
internal readonly struct CacheKey : IEquatable<CacheKey>
{
    /// <summary>
    /// Input function class/name.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Input function arguments.
    /// </summary>
    public IReadOnlySet<string> InputArguments { get; }

    /// <summary>
    /// Columns selected from input.
    /// </summary>
    public IReadOnlySet<string> SelectColumns { get; }

    /// <summary>
    /// Key conditions that were applied to the input.
    /// </summary>
    public IReadOnlySet<CacheKeyCondition> Conditions { get; }

    /// <summary>
    /// Applied offset.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Applied result limit. -1 means no limit.
    /// </summary>
    public long Limit { get; }

    internal CacheKey(
        string from,
        string[] inputArguments,
        string[] selectColumns,
        CacheKeyCondition[]? conditions = null,
        long offset = 0,
        long? limit = null)
    {
        From = from;
        InputArguments = inputArguments.Where(a => !string.IsNullOrEmpty(a)).ToHashSet();
        SelectColumns = selectColumns.ToHashSet();
        Conditions = (conditions ?? []).ToHashSet();
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

    internal static CacheKey Deserialize(string str, params IReadOnlyList<Column> columns)
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
    public override bool Equals(object? obj) => obj is CacheKey other && Equals(other);

    /// <inheritdoc />
    public bool Equals(CacheKey other) =>
        From == other.From
        && Offset == other.Offset
        && Limit == other.Limit
        && InputArguments.SetEquals(other.InputArguments)
        && SelectColumns.SetEquals(other.SelectColumns)
        && Conditions.SetEquals(other.Conditions);

    private static int GetSetHashCode<T>(IReadOnlySet<T> set)
    {
        var hashcode = default(HashCode);
        foreach (var setItem in set)
        {
            hashcode.Add(setItem);
        }
        return hashcode.ToHashCode();
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(From, GetSetHashCode(InputArguments),
        GetSetHashCode(SelectColumns), GetSetHashCode(Conditions), Offset, Limit);

    public static bool operator ==(CacheKey left, CacheKey right) => left.Equals(right);

    public static bool operator !=(CacheKey left, CacheKey right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => Serialize();
}
