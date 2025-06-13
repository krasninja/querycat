using System.Diagnostics;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Contains all necessary information to handle the query on all stages.
/// </summary>
[DebuggerDisplay("Id = {Id}, Iterator = {CurrentIterator}")]
internal sealed class SelectCommandContext(SelectQueryNode queryNode) : CommandContext
{
    public SelectQueryConditions Conditions { get; } = new();

    public IExecutionScope CapturedScope { get; set; } = NullExecutionScope.Instance;

    public bool IsSingleValue => CurrentIterator.Columns.Length == 1 && queryNode.IsSingleValue();

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
        SelectInputQueryContext? InputQueryContext);

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

        // Iterators.
        foreach (var context in GetParents())
        {
            index = context.CurrentIterator.GetColumnIndexByName(name, source);
            if (index > -1)
            {
                result = new InputNameSearchResult(context.CurrentIterator, index, context, null);
                return true;
            }

            // Rows inputs.
            foreach (var inputContext in context.Inputs)
            {
                index = inputContext.RowsInput.GetColumnIndexByName(name, source);
                if (index > -1)
                {
                    result = new InputNameSearchResult(inputContext.RowsInput, index, context, inputContext);
                    return true;
                }
            }
        }

        // Local CTE.
        foreach (var commonTableExpression in CteList)
        {
            index = commonTableExpression.RowsIterator.GetColumnIndexByName(name, source);
            if (index > -1)
            {
                result = new InputNameSearchResult(commonTableExpression.RowsIterator, index, this, null);
                return true;
            }
        }

        // Child context.
        foreach (var context in ChildContexts)
        {
            if (context.TryGetInputSourceByName(name, source, out var inputResult))
            {
                result = inputResult;
                return true;
            }
        }

        result = null;
        return false;
    }

    #endregion

    #region Inputs

    private readonly List<SelectInputQueryContext> _inputs = new();

    internal IReadOnlyList<SelectInputQueryContext> Inputs => _inputs;

    internal IRowsInput? FirstRowsInput => Inputs.Count > 0 ? Inputs[0].RowsInput : null;

    /// <summary>
    /// Add input source context.
    /// </summary>
    /// <param name="inputContext">Input context.</param>
    internal void AddInput(SelectInputQueryContext inputContext)
    {
        _inputs.Add(inputContext);
    }

    internal IEnumerable<SelectInputKeysConditions> GetAllConditionsColumns()
    {
        foreach (var inputContext in _inputs)
        {
            foreach (var condition in Conditions.GetConditionsColumns(inputContext.RowsInput, inputContext.Alias))
            {
                yield return condition;
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
    /// Remove child context.
    /// </summary>
    /// <param name="context">Context.</param>
    internal void RemoveChildContext(SelectCommandContext context)
    {
        context.Parent = null;
        _childContexts.Remove(context);
    }

    /// <summary>
    /// Set parent query context. The method also updates child items.
    /// If null - the context will be topmost.
    /// </summary>
    /// <param name="context">Parent context.</param>
    internal void SetParent(SelectCommandContext? context)
    {
        Parent = context;
        if (context != null)
        {
            CapturedScope = context.CapturedScope;
            context.AddChildContext(this);
        }
    }

    private IEnumerable<SelectCommandContext> GetParents()
    {
        yield return this;
        var parentContext = Parent;
        while (parentContext != null)
        {
            yield return parentContext;
            parentContext = parentContext.Parent;
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
    /// Set to <c>true</c> if there are no "SELECT *" matches.
    /// </summary>
    public bool HasExactColumnsSelect { get; set; }

    /// <summary>
    /// The function to evaluate arguments for output.
    /// </summary>
    public IFuncUnit? OutputArgumentsFunc { get; set; }

    /// <summary>
    /// Common table expressions of the query.
    /// </summary>
    internal List<CommonTableExpression> CteList { get; } = new();

    /// <summary>
    /// Returns the list of identifiers - direct references to input columns.
    /// </summary>
    /// <param name="sourceName">Source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of identifiers.</returns>
    internal async ValueTask<Column[]> GetSelectIdentifierColumnsAsync(string sourceName, CancellationToken cancellationToken)
    {
        var identifierAstVisitor = new IdentifierAstVisitor(sourceName);
        await identifierAstVisitor.RunAsync(queryNode, cancellationToken);
        return identifierAstVisitor.Columns.ToArray();
    }

    /// <summary>
    /// Dump debug info into instance of <see cref="IndentedStringBuilder" />.
    /// </summary>
    /// <param name="stringBuilder">String builder.</param>
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
        stringBuilder.AppendLine($"Query: {StringUtils.SafeSubstring(queryNode.ToString(), 0, 100)}");
        foreach (var childContext in ChildContexts)
        {
            stringBuilder.IncreaseIndent();
            childContext.Dump(stringBuilder);
            stringBuilder.DecreaseIndent();
        }
    }

    /// <summary>
    /// Close iterators and child resource.
    /// </summary>
    public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        foreach (var input in _inputs)
        {
            await input.RowsInput.CloseAsync(cancellationToken);
        }
        foreach (var childContext in ChildContexts)
        {
            await childContext.CloseAsync(cancellationToken);
        }
    }
}
