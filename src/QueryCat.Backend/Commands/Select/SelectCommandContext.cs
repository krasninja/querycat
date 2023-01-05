using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Contains all necessary information to handle the query on all stages.
/// </summary>
internal sealed class SelectCommandContext : CommandContext
{
    private SelectQueryNode _queryNode;

    public SelectCommandContext(SelectQueryNode queryNode)
    {
        _queryNode = queryNode;
    }

    #region Iterator

    private IRowsIterator? _currentIterator;

    /// <summary>
    /// Current iterator.
    /// </summary>
    public IRowsIterator CurrentIterator => _currentIterator ?? EmptyIterator.Instance;

    /// <summary>
    /// Append (overwrite) current iterator.
    /// </summary>
    /// <param name="nextIterator">The next iterator.</param>
    public void SetIterator(IRowsIterator nextIterator)
    {
        _currentIterator = nextIterator;
    }

    public record InputNameSearchResult(
        IRowsSchema Input,
        int ColumnIndex,
        SelectCommandContext Context,
        SelectCommandInputContext? InputContext);

    /// <summary>
    /// Try get input by name.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="source">Column source.</param>
    /// <param name="result">Search result, contains rows input, column index and found context.</param>
    /// <param name="options">Options.</param>
    /// <returns>Returns <c>true</c> if column is found, <c>false</c> otherwise.</returns>
    public bool TryGetInputSourceByName(string name, string source, out InputNameSearchResult? result,
        ColumnFindOptions options = ColumnFindOptions.Default)
    {
        int index;

        // Iterators.
        if (options.HasFlag(ColumnFindOptions.IncludeRowsIterators))
        {
            index = CurrentIterator.GetColumnIndexByName(name, source);
            if (index > -1)
            {
                result = new InputNameSearchResult(CurrentIterator, index, this, null);
                return true;
            }
        }

        // CTE.
        if (options.HasFlag(ColumnFindOptions.IncludeCommonTableExpressions))
        {
            foreach (var commonTableExpression in CteList)
            {
                foreach (var input in commonTableExpression.Context.Inputs)
                {
                    index = input.RowsInput.GetColumnIndexByName(name, source);
                    if (index > -1)
                    {
                        result = new InputNameSearchResult(input.RowsInput, index, commonTableExpression.Context, input);
                        return true;
                    }
                }
            }
        }

        // Inputs.
        if (options.HasFlag(ColumnFindOptions.IncludeInputSources))
        {
            foreach (var context in GetParents(context => context))
            {
                foreach (var input in context.Inputs)
                {
                    index = input.RowsInput.GetColumnIndexByName(name, source);
                    if (index > -1)
                    {
                        result = new InputNameSearchResult(input.RowsInput, index, context, input);
                        return true;
                    }
                }
            }
        }

        result = default;
        return false;
    }

    internal int GetAbsoluteColumnIndex(IRowsSchema input, int columnIndex)
    {
        if (input is IRowsInput rowsInput)
        {
            var absoluteIndex = 0;
            foreach (var inputContext in Inputs)
            {
                if (inputContext.RowsInput != rowsInput)
                {
                    absoluteIndex += inputContext.RowsInput.Columns.Length;
                    continue;
                }
                return columnIndex + absoluteIndex;
            }
            return -1;
        }

        if (input is IRowsIterator)
        {
            return columnIndex;
        }

        return -1;
    }

    #endregion

    #region Inputs

    /// <summary>
    /// The instance of <see cref="RowsInputIterator" /> that is used in FROM clause.
    /// </summary>
    public RowsInputIterator? RowsInputIterator { get; set; }

    private readonly List<SelectCommandInputContext> _inputs = new();

    internal IReadOnlyCollection<SelectCommandInputContext> Inputs => _inputs;

    /// <summary>
    /// Context information for rows inputs. We bypass this to input to provide additional information
    /// about a query. This would allow optimize execution.
    /// </summary>
    public IEnumerable<SelectInputQueryContext> InputQueryContextList => Inputs.Select(i => i.InputQueryContext);

    /// <summary>
    /// Add input source context.
    /// </summary>
    /// <param name="input">Input context.</param>
    internal void AddInput(SelectCommandInputContext input)
    {
        _inputs.Add(input);
    }

    #endregion

    #region Child-Parent

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
    /// Add child context.
    /// </summary>
    /// <param name="context">Context.</param>
    internal void AddChildContext(SelectCommandContext context)
    {
        context.Parent = this;
        _childContexts.Add(context);
    }

    /// <summary>
    /// Set parent query context. The method also updates child items.
    /// If null - the context will be topmost.
    /// </summary>
    /// <param name="context">Parent context.</param>
    internal void SetParent(SelectCommandContext? context)
    {
        Parent = context;
        Parent?.AddChildContext(this);
    }

    internal IEnumerable<T> GetParents<T>(Func<SelectCommandContext, T> func)
    {
        yield return func.Invoke(this);

        var parentContext = Parent;
        while (parentContext != null)
        {
            yield return func.Invoke(parentContext);
            parentContext = parentContext.Parent;
        }
    }

    internal IEnumerable<T> GetChildren<T>(Func<SelectCommandContext, T> func)
    {
        yield return func.Invoke(this);

        foreach (var child in _childContexts)
        {
            yield return func.Invoke(child);
        }
    }

    #endregion

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
    /// Common table expressions of the query.
    /// </summary>
    internal List<CommonTableExpression> CteList { get; } = new();

    #region CommandContext

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

    #endregion
}
