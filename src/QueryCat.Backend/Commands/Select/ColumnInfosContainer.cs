using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Container for <see cref="ColumnInfo" /> list.
/// </summary>
internal class ColumnsInfoContainer
{
    /// <summary>
    /// The additional information for the specific column for SELECT command handle.
    /// </summary>
    internal class ColumnInfo
    {
        /// <summary>
        /// The target column.
        /// </summary>
        public Column Column { get; }

        /// <summary>
        /// Related AST node.
        /// </summary>
        public SelectColumnsSublistNode? RelatedSelectSublistNode { get; set; }

        /// <summary>
        /// Column redirection. It indicates that redirect column value should be used
        /// instead of current.
        /// </summary>
        public Column? Redirect { get; }

        /// <summary>
        /// Is this column is used as aggregate key.
        /// For example "SELECT name FROM x GROUP BY x" the x is group column.
        /// </summary>
        public bool IsAggregateKey { get; set; }

        public ColumnInfo(Column column)
        {
            Column = column;
        }

        public ColumnInfo(ColumnInfo columnInfo)
        {
            this.Column = columnInfo.Column;
            this.Redirect = columnInfo.Redirect;
            this.IsAggregateKey = columnInfo.IsAggregateKey;
        }
    }

    private readonly List<ColumnInfo> _columns = new();

    /// <summary>
    /// Columns.
    /// </summary>
    public IReadOnlyList<ColumnInfo> Columns => _columns;

    public ColumnsInfoContainer()
    {
    }

    public ColumnsInfoContainer(ColumnsInfoContainer columnsInfoContainer)
    {
        _columns.AddRange(columnsInfoContainer.Columns.Select(c => new ColumnInfo(c)));
    }

    /// <summary>
    /// Get column information by column id. If it is not exists the default column info
    /// is used.
    /// </summary>
    /// <param name="column">The <see cref="Column" /> instance.</param>
    /// <returns>Column information.</returns>
    public ColumnInfo GetByColumn(Column column)
    {
        var info = _columns.Find(c => c.Column.Id == column.Id);
        if (info == null)
        {
            info = new ColumnInfo(column);
            _columns.Add(info);
        }
        return info;
    }

    /// <summary>
    /// Clear all container columns.
    /// </summary>
    public void Clear()
    {
        _columns.Clear();
    }
}
