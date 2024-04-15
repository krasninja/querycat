using System.Collections;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    public SelectCommandContext Context_Create(SelectQueryNode node, SelectCommandContext? parentContext = null)
    {
        if (node is SelectQuerySpecificationNode querySpecificationNode)
        {
            return Context_Create(querySpecificationNode, parentContext);
        }
        if (node is SelectQueryCombineNode queryCombineNode)
        {
            return Context_Create(queryCombineNode, parentContext);
        }
        throw new InvalidOperationException(string.Format(Resources.Errors.NotSupported, node.GetType().Name));
    }

    public SelectCommandContext Context_Create(SelectQuerySpecificationNode node, SelectCommandContext? parentContext = null)
    {
        Misc_Transform(node);
        var context = Context_CreateInitialContext(node, parentContext);
        Context_InitializeRowsInputs(context, node);
        ContextCte_PrepareInputList(context, node);
        Context_PrepareInitialInputs(context, node);
        return context;
    }

    public SelectCommandContext Context_Create(SelectQueryCombineNode node, SelectCommandContext? parentContext = null)
    {
        var context = Context_CreateInitialContext(node, parentContext);
        Context_InitializeRowsInputs(context, node);
        return context;
    }

    private SelectCommandContext Context_CreateInitialContext(SelectQueryNode node, SelectCommandContext? parentContext = null)
    {
        var context = new SelectCommandContext(node);
        context.CapturedScope = ExecutionThread.TopScope;
        context.SetParent(parentContext);
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
        return context;
    }

    private void Context_InitializeRowsInputs(SelectCommandContext context, SelectQueryNode node)
    {
        new CreateRowsInputVisitor(ExecutionThread, context).Run(node.GetChildren());
        if (node is SelectQuerySpecificationNode querySpecificationNode)
        {
            foreach (var input in context.Inputs)
            {
                FixInputColumnTypes(querySpecificationNode, input.RowsInput);
            }
        }
    }

    private void Context_PrepareInitialInput(SelectQueryNode queryNode)
    {
        var context = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        if (context.RowsInputIterator != null)
        {
            // Seems it has already been filled - skip.
            return;
        }

        if (queryNode is SelectQuerySpecificationNode querySpecificationNode)
        {
            Context_PrepareInitialInputs(context, querySpecificationNode);
            return;
        }
        if (queryNode is SelectQueryCombineNode queryCombineNode)
        {
            Context_PrepareInitialInput(context, queryCombineNode);
            return;
        }
        throw new InvalidOperationException($"{queryNode.GetType().Name} cannot be processed.");
    }

    private void Context_PrepareInitialInputs(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        // No FROM - assumed this is the query with SELECT only.
        if (querySpecificationNode.TableExpressionNode == null)
        {
            context.SetIterator(new SingleValueRowsInput().AsIterable());
            return;
        }

        // Start with FROM statement, if none - there is only one SELECT row.
        var finalRowsInputs = new List<IRowsInput>();
        foreach (var tableExpression in querySpecificationNode.TableExpressionNode.TablesNode.TableFunctionsNodes)
        {
            var rowsInputs = Context_GetRowsInputFromExpression(context, tableExpression);
            if (rowsInputs.Length == 0)
            {
                throw new QueryCatException(string.Format(Resources.Errors.CannotResolveInputSource, tableExpression));
            }
            var finalRowInput = rowsInputs.Last();
            var alias = tableExpression is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;

            Context_SetAlias(tableExpression, alias);
            finalRowInput = Context_WrapKeysInput(finalRowInput, context.Conditions);
            finalRowsInputs.Add(finalRowInput);
        }

        var resultRowsIterator = Context_CreateMultipleIterator(finalRowsInputs);
        context.RowsInputIterator = resultRowsIterator as RowsInputIterator;
        context.SetIterator(resultRowsIterator);
    }

    private void Context_PrepareInitialInput(
        SelectCommandContext context,
        SelectQueryCombineNode queryCombineNode)
    {
        Context_PrepareInitialInput(queryCombineNode.LeftQueryNode);
        var leftContext = queryCombineNode.LeftQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        Context_PrepareInitialInput(queryCombineNode.RightQueryNode);
        var rightContext = queryCombineNode.RightQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var combineRowsIterator = new CombineRowsIterator(
            leftContext.CurrentIterator,
            rightContext.CurrentIterator,
            Context_ConvertCombineType(queryCombineNode.CombineType),
            queryCombineNode.IsDistinct);
        context.SetIterator(combineRowsIterator);
    }

    private static CombineType Context_ConvertCombineType(SelectQueryCombineType combineType) => combineType switch
    {
        SelectQueryCombineType.Except => CombineType.Except,
        SelectQueryCombineType.Intersect => CombineType.Intersect,
        SelectQueryCombineType.Union => CombineType.Union,
        _ => throw new ArgumentOutOfRangeException(nameof(combineType), string.Format(Resources.Errors.NotImplemented, combineType)),
    };

    private IRowsInput[] Context_CreateInputSourceFromCte(SelectCommandContext context, IdentifierExpressionNode idNode)
    {
        var cteIndex = context.CteList.FindIndex(c => c.Name == idNode.FullName);
        if (cteIndex < 0)
        {
            return Array.Empty<IRowsInput>();
        }
        var inputs = new List<IRowsInput>
        {
            new RowsIteratorInput(context.CteList[cteIndex].RowsIterator),
        };
        if (idNode is ISelectAliasNode aliasNode)
        {
            Context_SetAlias(inputs[0], aliasNode.Alias);
        }
        if (idNode is SelectIdentifierExpressionNode selectIdentifierExpressionNode)
        {
            foreach (var joinedNode in selectIdentifierExpressionNode.JoinedNodes)
            {
                var joinRowsInput = Context_CreateInputSourceFromTableJoin(context, inputs[0], joinedNode);
                inputs.Add(joinRowsInput);
            }
        }
        return inputs.ToArray();
    }

    private IRowsInput[] Context_CreateInputSourceFromVariable(SelectCommandContext context, IdentifierExpressionNode idNode)
    {
        if (!context.CapturedScope.Contains(idNode.Name))
        {
            return [];
        }

        var value = Misc_CreateDelegate(idNode, context).Invoke();
        var internalValueType = value.GetInternalType();
        var rowsInputs = Array.Empty<IRowsInput>();

        if (internalValueType == DataType.Object && value.AsObjectUnsafe != null)
        {
            rowsInputs = Context_CreateInputSourceFromObjectVariable(context, value.AsObjectUnsafe);
        }
        else if (internalValueType == DataType.String && !string.IsNullOrEmpty(value.AsStringUnsafe))
        {
            rowsInputs = Context_CreateInputSourceFromStringVariable(context, value.AsStringUnsafe,
                (idNode as SelectIdentifierExpressionNode)?.Format);
        }

        // Alias.
        if (idNode is ISelectAliasNode selectAliasNode && !string.IsNullOrEmpty(selectAliasNode.Alias))
        {
            foreach (var rowsInput in rowsInputs)
            {
                Context_SetAlias(rowsInput, selectAliasNode.Alias);
            }
        }

        // Joined nodes processing.
        if (idNode is SelectIdentifierExpressionNode selectIdentifierExpressionNode)
        {
            var joinedInputs = new List<IRowsInput>(rowsInputs);
            foreach (var joinedNode in selectIdentifierExpressionNode.JoinedNodes)
            {
                var joinRowsInput = Context_CreateInputSourceFromTableJoin(context, rowsInputs[0], joinedNode);
                joinedInputs.Add(joinRowsInput);
            }
            rowsInputs = joinedInputs.ToArray();
        }

        return rowsInputs;
    }

    private IRowsInput[] Context_CreateInputSourceFromObjectVariable(SelectCommandContext currentContext, object objVariable)
    {
        IRowsInput? rowsInputResult = null;
        if (objVariable is IRowsInput rowsInput)
        {
            currentContext.AddInput(new SelectCommandInputContext(rowsInput));
            rowsInput.Open();
            rowsInputResult = rowsInput;
        }
        if (objVariable is IRowsIterator rowsIterator)
        {
            rowsInput = new RowsIteratorInput(rowsIterator);
            currentContext.AddInput(new SelectCommandInputContext(rowsInput));
            rowsInputResult = rowsInput;
        }
        if (objVariable is IEnumerable enumerable && enumerable.GetType().IsGenericType)
        {
#pragma warning disable IL2072
            rowsInput = new CollectionInput(GetUnderlyingType(enumerable.GetType()), enumerable);
#pragma warning restore IL2072
            currentContext.AddInput(new SelectCommandInputContext(rowsInput));
            rowsInputResult = rowsInput;
        }
        if (rowsInputResult == null)
        {
            return [];
        }
        return [rowsInputResult];
    }

    private IRowsInput[] Context_CreateInputSourceFromStringVariable(SelectCommandContext context,
        string strVariable, FunctionCallNode? formatterNode)
    {
        var args = new FunctionCallArguments()
            .Add("uri", new VariantValue(strVariable));
        if (formatterNode != null)
        {
            var formatter = Misc_CreateDelegate(formatterNode, context).Invoke();
            args.Add("fmt", formatter);
        }
        var rowsInput = ExecutionThread.FunctionsManager.CallFunction("read", ExecutionThread, args).As<IRowsInput>();
        rowsInput.QueryContext = new SelectInputQueryContext(rowsInput);
        rowsInput.Open();
        return [rowsInput];
    }

    // Last input is combine input.
    private IRowsInput[] Context_CreateInputSourceFromTableFunction(
        SelectCommandContext context,
        SelectTableFunctionNode tableFunctionNode)
    {
        var inputs = new List<IRowsInput>();
        var rowsInput = tableFunctionNode.GetRequiredAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey);
        inputs.Add(rowsInput);
        Context_SetAlias(rowsInput, tableFunctionNode.Alias);
        foreach (var joinedNode in tableFunctionNode.JoinedNodes)
        {
            rowsInput = Context_CreateInputSourceFromTableJoin(context, rowsInput, joinedNode);
            inputs.Add(rowsInput);
        }
        return inputs.ToArray();
    }

    private IRowsInput Context_CreateInputSourceFromTableJoin(SelectCommandContext context, IRowsInput left,
        SelectTableJoinedNode tableJoinedNode)
    {
        var right = Context_GetRowsInputFromExpression(context, tableJoinedNode.RightTableNode).Last();
        var alias = tableJoinedNode.RightTableNode is ISelectAliasNode selectAlias ? selectAlias.Alias : string.Empty;
        Context_SetAlias(right, alias);
        right = Context_WrapKeysInput(right, context.Conditions);
        left = Context_WrapKeysInput(left, context.Conditions);

        // For right join we swap left and right. But we keep columns in the same order.
        var join = Context_ConvertAstJoinType(tableJoinedNode.JoinTypeNode.JoinedType);
        var reverseColumnsOrder = false;
        if (join == JoinType.Right)
        {
            (left, right) = (right, left);
            reverseColumnsOrder = true;
        }
        // Because of iterator specific conditions we better cache right input. Consider that resetting rows input
        // might be resource consuming operation.
        if (!ExecutionThread.Options.DisableCache && Context_CanUseInputCache(right))
        {
            right = new CacheRowsInput(right, context.Conditions);
        }

        if (tableJoinedNode is SelectTableJoinedOnNode joinedOnNode)
        {
            var searchFunc = new InputCreateDelegateVisitor(ExecutionThread, context, left, right)
                .RunAndReturn(joinedOnNode.SearchConditionNode);
            return new SelectJoinRowsInput(left, right, join, searchFunc, reverseColumnsOrder);
        }
        if (tableJoinedNode is SelectTableJoinedUsingNode joinedUsingNode)
        {
            var searchFunc = new InputCreateDelegateVisitor(ExecutionThread, context, left, right)
                .RunAndReturn(joinedUsingNode);
            return new SelectJoinRowsInput(left, right, join, searchFunc, reverseColumnsOrder);
        }
        throw new ArgumentException(string.Format(Resources.Errors.NotSupported, tableJoinedNode.GetType().Name),
            nameof(tableJoinedNode));
    }

    private IRowsInput Context_WrapKeysInput(IRowsInput rowsInput, SelectQueryConditions conditions)
    {
        if (rowsInput is IRowsInputKeys rowsInputKeys
            && rowsInputKeys is not SetKeysRowsInput)
        {
            return new SetKeysRowsInput(rowsInputKeys, conditions);
        }
        return rowsInput;
    }

    private List<IRowsInput> Context_WrapKeysInput(IReadOnlyList<IRowsInput> rowsInputs, SelectQueryConditions conditions)
        => rowsInputs.Select(ri => Context_WrapKeysInput(ri, conditions)).ToList();

    private IRowsInput Context_CreateInputSourceFromTable(SelectCommandContext context,
        SelectTableNode tableNode)
    {
        var func = new SelectCreateDelegateVisitor(ExecutionThread, context)
            .RunAndReturn(tableNode);
        var rowsFrame = func.Invoke().As<RowsFrame>();
        return new RowsIteratorInput(rowsFrame.GetIterator());
    }

    private IRowsInput[] Context_GetRowsInputFromExpression(SelectCommandContext context, ExpressionNode expressionNode)
    {
        if (expressionNode is SelectQueryNode queryNode)
        {
            return
            [
                Context_CreateInputSourceFromSubQuery(context, queryNode)
            ];
        }
        if (expressionNode is SelectTableFunctionNode tableFunctionNode)
        {
            return Context_CreateInputSourceFromTableFunction(context, tableFunctionNode);
        }
        if (expressionNode is IdentifierExpressionNode idNode)
        {
            var inputs = Context_CreateInputSourceFromCte(context, idNode);
            if (inputs.Length == 0)
            {
                inputs = Context_CreateInputSourceFromVariable(context, idNode);
            }
            return inputs;
        }
        if (expressionNode is SelectTableNode tableNode)
        {
            return
            [
                Context_CreateInputSourceFromTable(context, tableNode)
            ];
        }

        throw new InvalidOperationException($"Cannot process node '{expressionNode}' as input.");
    }

    private IRowsInput Context_CreateInputSourceFromSubQuery(SelectCommandContext context, SelectQueryNode queryNode)
    {
        var rowsIterator = CreateIterator(queryNode, parentContext: context);

        var rowsInput = new RowsIteratorInput(rowsIterator);
        context.AddInput(new SelectCommandInputContext(rowsInput));
        Context_SetAlias(rowsInput, queryNode.Alias);
        return rowsInput;
    }

    private static IRowsIterator Context_CreateMultipleIterator(List<IRowsInput> rowsInputs)
    {
        if (rowsInputs.Count == 0)
        {
            throw new QueryCatException(Resources.Errors.NoInputs);
        }
        if (rowsInputs.Count == 1)
        {
            return new RowsInputIterator(rowsInputs[0], autoFetch: false);
        }
        var multipleIterator = new MultiplyRowsIterator(
            new RowsInputIterator(rowsInputs[0], autoFetch: true),
            new RowsInputIterator(rowsInputs[1], autoFetch: true));
        for (int i = 2; i < rowsInputs.Count; i++)
        {
            multipleIterator = new MultiplyRowsIterator(
                multipleIterator, new RowsInputIterator(rowsInputs[i], autoFetch: true));
        }
        return multipleIterator;
    }

    private static void Context_SetAlias(IAstNode node, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }

        foreach (var inputColumn in node.GetAllChildren<IdentifierExpressionNode>()
                     .Where(n => string.IsNullOrEmpty(n.TableSourceName)))
        {
            inputColumn.TableSourceName = alias;
        }

        var iterator = node.GetAttribute<IRowsIterator>(AstAttributeKeys.ResultKey);
        if (iterator != null)
        {
            foreach (var column in iterator.Columns)
            {
                column.SourceName = alias;
            }
        }
    }

    private static void Context_SetAlias(IRowsInput input, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }
        foreach (var column in input.Columns)
        {
            column.SourceName = alias;
        }
    }

    /// <summary>
    /// Find the expressions in SELECT output area like CAST(id AS string).
    /// </summary>
    private void FixInputColumnTypes(SelectQuerySpecificationNode querySpecificationNode, IRowsInput rowsInput)
    {
        foreach (var castNode in querySpecificationNode.ColumnsListNode.GetAllChildren<CastFunctionNode>())
        {
            if (castNode.ExpressionNode is not IdentifierExpressionNode idNode)
            {
                continue;
            }

            var columnIndex = rowsInput.GetColumnIndexByName(idNode.TableFieldName, idNode.TableSourceName);
            if (columnIndex > -1)
            {
                rowsInput.Columns[columnIndex].DataType = castNode.TargetTypeNode.Type;
            }
        }
    }

    /// <summary>
    /// The function determines if cache can be applied for the certain input.
    /// </summary>
    /// <param name="input">Rows input.</param>
    /// <returns><c>True</c> if cache can be applied, <c>false</c> otherwise.</returns>
    private static bool Context_CanUseInputCache(IRowsInput input)
    {
        if (input is not IRowsIteratorParent rowsIteratorRoot)
        {
            return true;
        }

        var childNodes = rowsIteratorRoot.GetChildren().ToList();
        while (childNodes.Any())
        {
            // If we have any proxy iterator we cannot guarantee inner input persistence.
            foreach (var child in childNodes)
            {
                if (child is ProxyRowsIterator)
                {
                    return false;
                }
            }
            // If all child nodes are already cache inputs - no need to cache twice.
            if (childNodes.All(n => n is CacheRowsInput))
            {
                return false;
            }

            childNodes = childNodes
                .OfType<IRowsIteratorParent>()
                .SelectMany(n => n.GetChildren())
                .ToList();
        }

        return true;
    }

    private static JoinType Context_ConvertAstJoinType(SelectTableJoinedType tableJoinedType)
        => tableJoinedType switch
        {
            SelectTableJoinedType.Full => JoinType.Full,
            SelectTableJoinedType.Inner => JoinType.Inner,
            SelectTableJoinedType.Left => JoinType.Left,
            SelectTableJoinedType.Right => JoinType.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(tableJoinedType), tableJoinedType, null)
        };

    private static Type GetUnderlyingType(Type type)
    {
        if (type.IsGenericType)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[0];
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[1];
            }
        }
        return type;
    }
}
