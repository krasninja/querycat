using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Contains all necessary information to handle the query on all stages.
/// </summary>
internal sealed class SelectCommandContext : IDisposable
{
    private static int nextId;

    private readonly SelectQueryNode _queryNode;

    internal int Id { get; } = Interlocked.Increment(ref nextId);

    public SelectQueryConditions Conditions { get; } = new();

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
        SelectCommandContext Context);

    /// <summary>
    /// Try get input by name.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="source">Column source.</param>
    /// <param name="result">Search result, contains rows input, column index and found context.</param>
    /// <returns>Returns <c>true</c> if column is found, <c>false</c> otherwise.</returns>
    public bool TryGetInputSourceByName(string name, string source, out InputNameSearchResult? result)
    {
        int index;

        // Local CTE.
        foreach (var commonTableExpression in CteList)
        {
            index = commonTableExpression.RowsIterator.GetColumnIndexByName(name, source);
            if (index > -1)
            {
                result = new InputNameSearchResult(
                    commonTableExpression.RowsIterator, index, this);
                return true;
            }
        }

        // Iterators.
        foreach (var context in GetParents(context => context))
        {
            index = context.CurrentIterator.GetColumnIndexByName(name, source);
            if (index > -1)
            {
                result = new InputNameSearchResult(context.CurrentIterator, index, context);
                return true;
            }
        }

        result = default;
        return false;
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

    internal IEnumerable<SelectInputKeysConditions> GetConditionsColumns()
    {
        foreach (var inputContext in _inputs)
        {
            if (inputContext.RowsInput is not IRowsInputKeys inputKeys)
            {
                continue;
            }
            foreach (var keyColumn in inputKeys.GetKeyColumns())
            {
                var column = inputKeys.GetColumnByName(keyColumn.ColumnName);
                if (column == null)
                {
                    throw new QueryCatException($"Cannot find key column '{keyColumn.ColumnName}'.");
                }
                var matchConditions = Conditions
                    .Where(c =>
                        Column.NameEquals(c.Column, keyColumn.ColumnName)
                        && column.SourceName == inputContext.Alias
                        && keyColumn.Operations.Contains(c.Operation))
                    .ToArray();
                yield return new SelectInputKeysConditions(inputKeys, column, keyColumn, matchConditions);
            }
        }
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

    public void Dump(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine();
        var columns = string.Join(", ", CurrentIterator.Columns.Select(c => c.ToString()));
        stringBuilder.AppendLine($"Id: {Id}");
        if (Parent != null)
        {
            stringBuilder.AppendLine($"Parent: {Parent?.Id}");
        }
        if (_childContexts.Any())
        {
            stringBuilder.AppendLine($"Children: {string.Join(", ", _childContexts.Select(c => c.Id))}");
        }
        stringBuilder.AppendLine($"Output: {columns}");
        stringBuilder.AppendLine($"Query: {StringUtils.SafeSubstring(_queryNode.ToString(), 0, 100)}");
        foreach (var childContext in ChildContexts)
        {
            stringBuilder.IncreaseIndent();
            childContext.Dump(stringBuilder);
            stringBuilder.DecreaseIndent();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        RowsInputIterator?.Dispose();
        foreach (var childContext in ChildContexts)
        {
            childContext.Dispose();
        }
    }
}
