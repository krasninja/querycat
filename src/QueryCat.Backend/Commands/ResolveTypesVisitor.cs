using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
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
        var traversal = new AstTraversal(this);
        traversal.PostOrder(node);
    }

    public static FunctionArgumentsTypes CreateFunctionArgumentsTypes(
        IList<FunctionCallArgumentNode> callArguments)
    {
        var positionalArgumentsTypes = callArguments
            .Where(arg => arg.IsPositional)
            .Select(arg => new KeyValuePair<int, DataType>(
                callArguments.IndexOf(arg),
                arg.ExpressionValue.GetDataType()))
            .ToArray();
        var namedArgumentsTypes = callArguments
            .Where(arg => !arg.IsPositional)
            .Select(arg => new KeyValuePair<string, DataType>(
                arg.Key!, arg.ExpressionValue.GetDataType()))
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
                string.Format(Resources.Errors.CannotApplyOperator, VariantValue.Operation.Between, leftType, rightType));
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
                string.Format(Resources.Errors.CannotApplyOperator, node.Operation, leftType, rightType));
        }
        node.SetDataType(targetType);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        throw new CannotFindIdentifierException(node.Name);
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        node.SetAttribute(Constants.TypeKey, node.Value.GetInternalType());
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        if (node.Operation == VariantValue.Operation.IsNull
            || node.Operation == VariantValue.Operation.IsNotNull)
        {
            node.SetDataType(DataType.Boolean);
        }
        else
        {
            node.Right.CopyTo<DataType>(Constants.TypeKey, node);
        }
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        node.SetDataType(DataType.Boolean);
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        node.ExpressionValue.CopyTo<DataType>(Constants.TypeKey, node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallExpressionNode node)
    {
        node.CopyTo<DataType>(Constants.TypeKey, node.FunctionNode);
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
                var argType = callArgumentNode.ExpressionValue.GetDataType();
                if (DataTypeUtils.RowDataTypes.Contains(argType))
                {
                    returnType = argType;
                    break;
                }
            }
        }

        node.SetAttribute(Constants.FunctionKey, function);
        node.SetDataType(returnType);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        node.CopyTo<DataType>(Constants.TypeKey, node.FunctionNode);
    }

    #endregion
}
