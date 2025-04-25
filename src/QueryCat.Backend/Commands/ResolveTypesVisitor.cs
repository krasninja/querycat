using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Resolve types for literals, function calls, expressions.
/// </summary>
internal class ResolveTypesVisitor : AstVisitor
{
    protected IExecutionThread<ExecutionOptions> ExecutionThread { get; }

    public ResolveTypesVisitor(IExecutionThread<ExecutionOptions> executionThread)
    {
        ExecutionThread = executionThread;
    }

    /// <inheritdoc />
    public override ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        return AstTraversal.PostOrderAsync(node, cancellationToken);
    }

    /// <summary>
    /// Separate names and positional arguments.
    /// </summary>
    /// <param name="callArguments">Call arguments.</param>
    /// <returns>Instance of <see cref="FunctionCallArgumentsTypes" />.</returns>
    public static FunctionCallArgumentsTypes CreateFunctionArgumentsTypes(
        IList<FunctionCallArgumentNode> callArguments)
    {
        var positionalArgs = new List<KeyValuePair<int, DataType>>(callArguments.Count);
        var namedArgs = new List<KeyValuePair<string, DataType>>(callArguments.Count);
        for (var i = 0; i < callArguments.Count; i++)
        {
            var arg = callArguments[i];
            if (callArguments[i].IsPositional)
            {
                positionalArgs.Add(new KeyValuePair<int, DataType>(i, arg.ExpressionValueNode.GetDataType()));
            }
            else
            {
                namedArgs.Add(new KeyValuePair<string, DataType>(arg.Key!, arg.ExpressionValueNode.GetDataType()));
            }
        }

        return new FunctionCallArgumentsTypes(
            positionalArgs.ToArray(),
            namedArgs.ToArray()
        );
    }

    #region General

    /// <inheritdoc />
    public override ValueTask VisitAsync(BetweenExpressionNode node, CancellationToken cancellationToken)
    {
        var leftType = node.Left.GetDataType();
        var rightType = node.Right.GetDataType();
        var targetType = VariantValue.GetResultType(leftType, rightType, VariantValue.Operation.Between);
        if (targetType == DataType.Void)
        {
            throw new SemanticException(
                string.Format(Resources.Errors.CannotApplyOperationForTypes, VariantValue.Operation.Between, leftType, rightType));
        }
        node.SetDataType(targetType);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        var leftType = node.LeftNode.GetDataType();
        var rightType = node.RightNode.GetDataType();
        var targetType = VariantValue.GetResultType(leftType, rightType, node.Operation);
        if (targetType == DataType.Void)
        {
            throw new SemanticException(
                string.Format(Resources.Errors.CannotApplyOperationForTypes, node.Operation, leftType, rightType));
        }
        node.SetDataType(targetType);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(CaseExpressionNode node, CancellationToken cancellationToken)
    {
        var lastWhenNode = node.WhenNodes.LastOrDefault();
        if (lastWhenNode != null)
        {
            node.SetDataType(lastWhenNode.ResultNode.GetDataType());
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (SetDataTypeFromVariable(node, node.Name))
        {
            return ValueTask.CompletedTask;
        }

        throw new CannotFindIdentifierException(node.FullName);
    }

    protected bool SetDataTypeFromVariable(IdentifierExpressionNode node, string name)
    {
        if (node.HasAttribute(AstAttributeKeys.TypeKey))
        {
            return true;
        }

        var scope = ExecutionThread.TopScope;
        if (ExecutionThread.TryGetVariable(name, out var value, scope))
        {
            var valueType = value.Type;
            if (valueType == DataType.Object && node.HasSelectors)
            {
                node.SetAttribute(AstAttributeKeys.TypeKey, DataType.Dynamic);
            }
            else
            {
                node.SetAttribute(AstAttributeKeys.TypeKey, valueType);
            }
            return true;
        }
        if (node.IsCurrentSpecialIdentifier)
        {
            node.SetAttribute(AstAttributeKeys.TypeKey, DataType.Dynamic);
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        node.SetAttribute(AstAttributeKeys.TypeKey, node.Value.Type);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(UnaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        if (node.Operation == VariantValue.Operation.IsNull
            || node.Operation == VariantValue.Operation.IsNotNull
            || node.Operation == VariantValue.Operation.Not)
        {
            node.SetDataType(DataType.Boolean);
        }
        else
        {
            node.RightNode.CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        node.SetDataType(DataType.Boolean);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(AtTimeZoneNode node, CancellationToken cancellationToken)
    {
        node.SetDataType(DataType.Timestamp);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Special functions

    /// <inheritdoc />
    public override ValueTask VisitAsync(CastFunctionNode node, CancellationToken cancellationToken)
    {
        node.SetDataType(node.TargetTypeNode.Type);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(CoalesceFunctionNode node, CancellationToken cancellationToken)
    {
        var types = node.Expressions.Select(e => e.GetDataType());
        var generalType = types.Where(DataTypeUtils.IsSimple).Distinct().ToList();
        if (generalType.Count > 1)
        {
            var foundTypes = string.Join(", ", generalType);
            throw new SemanticException(string.Format(Resources.Errors.CoalesceMustHaveSameArguments, foundTypes));
        }
        node.SetDataType(generalType.First());
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallArgumentNode node, CancellationToken cancellationToken)
    {
        node.ExpressionValueNode.CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallExpressionNode node, CancellationToken cancellationToken)
    {
        node.CopyTo<DataType>(AstAttributeKeys.TypeKey, node.FunctionNode);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        VisitFunctionCallNode(node);
        return ValueTask.CompletedTask;
    }

    public IFunction VisitFunctionCallNode(FunctionCallNode node)
    {
        var functionArgumentsTypes = CreateFunctionArgumentsTypes(node.Arguments);
        var function = ExecutionThread.FunctionsManager.FindByNameFirst(
            node.FunctionName, functionArgumentsTypes);
        var returnType = function.ReturnType;

        // If return type is VOID, we try to determine type by call arguments.
        // It is needed for example for COALESCE function.
        if (returnType == DataType.Void)
        {
            foreach (var callArgumentNode in node.Arguments)
            {
                var argType = callArgumentNode.ExpressionValueNode.GetDataType();
                if (DataTypeUtils.RowDataTypes.Contains(argType))
                {
                    returnType = argType;
                    break;
                }
            }
        }

        node.SetAttribute(AstAttributeKeys.FunctionKey, function);
        node.SetDataType(returnType);

        return function;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        node.CopyTo<DataType>(AstAttributeKeys.TypeKey, node.FunctionNode);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Select Command

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        await new SelectPlanner(ExecutionThread, this).CreateIteratorAsync(node, cancellationToken: cancellationToken);
        node.ColumnsListNode.ColumnsNodes[0].CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        await new SelectPlanner(ExecutionThread, this).CreateIteratorAsync(node, cancellationToken: cancellationToken);
        node.ColumnsListNode.ColumnsNodes[0].CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
    }

    #endregion

    #region Declare

    /// <inheritdoc />
    public override ValueTask VisitAsync(DeclareNode node, CancellationToken cancellationToken)
    {
        if (node.ValueNode != null)
        {
            node.SetDataType(node.ValueNode.GetDataType());
        }
        return ValueTask.CompletedTask;
    }

    #endregion
}
