using System.Text;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The cache key for the specific instance of <see cref="IRowsInput" />.
/// </summary>
internal class CacheKey
{
    public string From { get; }

    public string[] SelectColumns { get; }

    public QueryContextCondition[] Conditions { get; }

    public int Offset { get; }

    public int Limit { get; }

    public CacheKey(
        string from,
        string[] selectColumns,
        QueryContextCondition[]? conditions = null,
        int offset = 0,
        int limit = -1)
    {
        From = from;
        SelectColumns = selectColumns;
        Conditions = conditions ?? Array.Empty<QueryContextCondition>();
        Offset = offset;
        Limit = limit;
    }

    #region Serialization

    internal string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append(StringUtils.Quote($"F:{From}").ToString());
        if (SelectColumns.Length > 0)
        {
            sb.Append(' ');
            var columns = SelectColumns.Select(c => "S:" + StringUtils.Quote(c).ToString());
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
        if (Conditions.Length > 0)
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
        List<QueryContextCondition> conditions = new();
        foreach (var item in arr)
        {
            if (item.StartsWith("F:"))
            {
                from = item.Substring(2);
            }
            else if (item.StartsWith("S:"))
            {
                selectColumns.Add(item.Substring(2));
            }
            else if (item.StartsWith("W:"))
            {
                var condition = QueryContextCondition.CreateFromString(FindColumnByName, item.Substring(2));
                if (condition != null)
                {
                    conditions.Add(condition);
                }
            }
            else if (item.StartsWith("O:"))
            {
                offset = int.Parse(item.Substring(2));
            }
            else if (item.StartsWith("L:"))
            {
                limit = int.Parse(item.Substring(2));
            }
        }
        return new CacheKey(from, selectColumns.ToArray(), conditions.ToArray(), offset, limit);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() => Serialize();
}
