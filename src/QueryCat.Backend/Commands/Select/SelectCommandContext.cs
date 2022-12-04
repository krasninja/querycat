using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Contains all necessary information to handle the query on all stages.
/// </summary>
internal sealed class SelectCommandContext : CommandContext
{
    /// <summary>
    /// Current iterator.
    /// </summary>
    public IRowsIterator CurrentIterator { get; private set; }

    /// <summary>
    /// The instance of <see cref="RowsInputIterator" /> that is used in FROM clause.
    /// </summary>
    public RowsInputIterator? RowsInputIterator { get; set; }

    internal HashSet<int> PrefetchedColumnIndexes { get; } = new();

    /// <summary>
    /// Parent select context.
    /// </summary>
    public SelectCommandContext? Parent { get; private set; }

    private readonly List<SelectCommandContext> _childContexts = new();

    /// <summary>
    /// Child command contexts.
    /// </summary>
    public IReadOnlyList<SelectCommandContext> ChildContexts => _childContexts;

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
    /// The function to evaluate arguments for output.
    /// </summary>
    public IFuncUnit? OutputArgumentsFunc { get; set; }

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

    /// <summary>
    /// Add child context.
    /// </summary>
    /// <param name="context">Context.</param>
    internal void AddChildContext(SelectCommandContext context)
        => _childContexts.Add(context);

    /// <summary>
    /// Add child contexts list.
    /// </summary>
    /// <param name="contexts">Enumerable of contexts.</param>
    internal void AddChildContext(IEnumerable<SelectCommandContext> contexts)
    {
        foreach (var selectCommandContext in contexts)
        {
            AddChildContext(selectCommandContext);
        }
    }

    internal void SetParent(SelectCommandContext context)
    {
        Parent = context;
        Parent.AddChildContext(this);
    }

    /// <summary>
    /// Get column index by name and return relate rows iterator.
    /// It also search within parent contexts.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="source">Source name.</param>
    /// <param name="rowsIterator">Related rows iterator.</param>
    /// <returns>Column index or -1.</returns>
    public int GetColumnIndexByName(string name, string source, out IRowsIterator? rowsIterator)
    {
        foreach (var iterator in GetAllIterators())
        {
            var columnIndex = iterator.GetColumnIndexByName(name, source);
            if (columnIndex > -1)
            {
                rowsIterator = iterator;
                return columnIndex;
            }
        }

        rowsIterator = default;
        return -1;
    }

    private IEnumerable<IRowsIterator> GetAllIterators()
    {
        yield return CurrentIterator;
        var parentContext = Parent;
        while (parentContext != null)
        {
            yield return parentContext.CurrentIterator;
            parentContext = parentContext.Parent;
        }
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        return VariantValue.CreateFromObject(CurrentIterator);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        RowsInputIterator?.Dispose();
        foreach (var childContext in ChildContexts)
        {
            childContext.Dispose();
        }
        base.Dispose(disposing);
    }
}
