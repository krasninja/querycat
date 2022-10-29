using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Contains all necessary information to handle the query on all stages.
/// </summary>
internal sealed class SelectCommandContext
{
    /// <summary>
    /// Current iterator.
    /// </summary>
    public IRowsIterator CurrentIterator { get; private set; }

    /// <summary>
    /// The instance of <see cref="RowsInputIterator" /> that is used in FROM clause.
    /// </summary>
    public RowsInputIterator? RowsInputIterator { get; set; }

    /// <summary>
    /// Parent select contexts.
    /// </summary>
    public SelectCommandContext[] ParentContexts { get; set; } = Array.Empty<SelectCommandContext>();

    /// <summary>
    /// Context information for rows inputs. We bypass this to input to provide additional information
    /// about a query. This would allow optimize execution.
    /// </summary>
    public SelectInputQueryContext[] InputQueryContextList { get; set; } = Array.Empty<SelectInputQueryContext>();

    /// <summary>
    /// Container to get columns additional information.
    /// </summary>
    public ColumnsInfoContainer ColumnsInfoContainer { get; } = new();

    /// <summary>
    /// Has INTO clause. In that case we do not return output value.
    /// </summary>
    public bool HasOutput { get; set; }

    /// <summary>
    /// Set to <c>true</c> if the final result was prepared.
    /// </summary>
    public bool HasFinalRowsIterator { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="iterator">Input iterator.</param>
    public SelectCommandContext(IRowsIterator iterator)
    {
        CurrentIterator = iterator;
    }

    /// <summary>
    /// Append (overwrite) current iterator.
    /// </summary>
    /// <param name="nextIterator">The next iterator.</param>
    public void SetIterator(IRowsIterator nextIterator)
    {
        CurrentIterator = nextIterator;
    }

    public int GetColumnIndexByName(string name, string source, out IRowsIterator? rowsIterator)
    {
        var columnIndex = CurrentIterator.GetColumnIndexByName(name, source);
        if (columnIndex > -1)
        {
            rowsIterator = CurrentIterator;
            return columnIndex;
        }
        foreach (var parentContext in ParentContexts)
        {
            columnIndex = parentContext.CurrentIterator.GetColumnIndexByName(name, source);
            if (columnIndex > -1)
            {
                rowsIterator = parentContext.CurrentIterator;
                return columnIndex;
            }
        }

        rowsIterator = default;
        return -1;
    }
}
