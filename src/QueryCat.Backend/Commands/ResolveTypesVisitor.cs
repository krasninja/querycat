using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Resolve types for literals, function calls, expressions.
/// </summary>
internal class ResolveTypesVisitor : AstVisitor
{
    protected ExecutionThread ExecutionThread { get; }

    public ResolveTypesVisitor(ExecutionThread executionThread)
    {
        ExecutionThread = executionThread;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PostOrder(node);
    }

    public static FunctionArgumentsTypes CreateFunctionArgumentsTypes(
        IList<FunctionCallArgumentNode> callArguments)
    {
        var positionalArgumentsTypes = callArguments
            .Where(arg => arg.IsPositional)
            .Select(arg => new KeyValuePair<int, DataType>(
                callArguments.IndexOf(arg),
                arg.ExpressionValueNode.GetDataType()))
            .ToArray();
        var namedArgumentsTypes = callArguments
            .Where(arg => !arg.IsPositional)
            .Select(arg => new KeyValuePair<string, DataType>(
                arg.Key!, arg.ExpressionValueNode.GetDataType()))
            .ToArray();
        return new FunctionArgumentsTypes(
            positionalArgumentsTypes,
            namedArgumentsTypes
        );
    }

    #region General

    /// <inheritdoc />
    public override void Visit(BetweenExpressionNode node)
    {
        var leftType = node.Left.GetDataType();
        var rightType = node.Right.GetDataType();
        var targetType = VariantValue.GetResultType(leftType, rightType, VariantValue.Operation.Between);
        if (targetType == DataType.Void)
        {
            throw new SemanticException(
                $"Cannot apply operation '{VariantValue.Operation.Between}' to arguments of types {leftType} and {rightType}.");
        }
        node.SetDataType(targetType);
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        var leftType = node.Left.GetDataType();
        var rightType = node.Right.GetDataType();
        var targetType = VariantValue.GetResultType(leftType, rightType, node.Operation);
        if (targetType == DataType.Void)
        {
            throw new SemanticException(
                $"Cannot apply operation '{node.Operation}' to arguments of types {leftType} and {rightType}.");
        }
        node.SetDataType(targetType);
    }

    /// <inheritdoc />
    public override void Visit(CaseExpressionNode node)
    {
        var lastWhenNode = node.WhenNodes.LastOrDefault();
        if (lastWhenNode != null)
        {
            node.SetDataType(lastWhenNode.ResultNode.GetDataType());
        }
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        if (string.IsNullOrEmpty(node.SourceName))
        {
            var varIndex = ExecutionThread.RootScope.GetVariableIndex(node.Name, out var scope);
            if (varIndex > -1)
            {
                node.SetAttribute(AstAttributeKeys.TypeKey, scope!.Variables[varIndex].GetInternalType());
                return;
            }
        }
        throw new CannotFindIdentifierException(node.FullName);
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        node.SetAttribute(AstAttributeKeys.TypeKey, node.Value.GetInternalType());
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        if (node.Operation == VariantValue.Operation.IsNull
            || node.Operation == VariantValue.Operation.IsNotNull
            || node.Operation == VariantValue.Operation.Not)
        {
            node.SetDataType(DataType.Boolean);
        }
        else
        {
            node.Right.CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
        }
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        node.SetDataType(DataType.Boolean);
    }

    #endregion

    #region Special functions

    /// <inheritdoc />
    public override void Visit(CastFunctionNode node)
    {
        node.SetDataType(node.TargetTypeNode.Type);
    }

    /// <inheritdoc />
    public override void Visit(CoalesceFunctionNode node)
    {
        var types = node.Expressions.Select(e => e.GetDataType());
        var generalType = types.Where(DataTypeUtils.IsSimple).Distinct().ToList();
        if (generalType.Count > 1)
        {
            var foundTypes = string.Join(", ", generalType);
            throw new SemanticException($"COALESCE function must have identical argument types. Found ({foundTypes}).");
        }
        node.SetDataType(generalType.First());
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        node.ExpressionValueNode.CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallExpressionNode node)
    {
        node.CopyTo<DataType>(AstAttributeKeys.TypeKey, node.FunctionNode);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        var functionArgumentsTypes = CreateFunctionArgumentsTypes(node.Arguments);
        var function = ExecutionThread.FunctionsManager.FindByName(
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
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        node.CopyTo<DataType>(AstAttributeKeys.TypeKey, node.FunctionNode);
    }

    #endregion
}
