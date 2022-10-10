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
    public IRowsIterator CurrentIterator { get; set; }

    /// <summary>
    /// The instance of <see cref="RowsInputIterator" /> that is used in FROM clause.
    /// </summary>
    public RowsInputIterator? RowsInputIterator { get; set; }

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
    public void AppendIterator(IRowsIterator nextIterator)
    {
        CurrentIterator = nextIterator;
    }
}
