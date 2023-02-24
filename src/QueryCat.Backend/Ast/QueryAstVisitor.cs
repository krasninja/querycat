using System.Text;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Serialize the AST representation of query into a text string.
/// </summary>
internal class QueryAstVisitor : AstVisitor
{
    private const char Space = ' ';

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        var traversal = new AstTraversal(this);
        traversal.PostOrder(node);
    }

    #region General

    /// <inheritdoc />
    public override void Visit(BetweenExpressionNode node)
    {
        SetString(node,
            $"{GetStringWithParens(node.Expression)} BETWEEN {GetStringWithParens(node.Left)} AND {GetStringWithParens(node.Right)}");
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        SetString(node,
            $"{GetStringWithParens(node.LeftNode)} {GetOperationString(node.Operation)} {GetStringWithParens(node.RightNode)}");
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        SetString(node, node.Name);
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(InExpressionValuesNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        var value = node.Value.ToString();
        if (node.Value.GetInternalType() == DataType.String)
        {
            value = string.Concat("'", value, "'");
        }
        SetString(node, value);
    }

    /// <inheritdoc />
    public override void Visit(ProgramNode node)
    {
        SetString(node, string.Join("; ", node.Statements.Select(GetString)));
    }

    /// <inheritdoc />
    public override void Visit(TernaryOperationExpressionNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(TypeNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        SetString(node, $"{GetOperationString(node.Operation)}{GetString(node.RightNode)}");
    }

    #endregion

    #region Echo

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
    {
        SetString(node, $"ECHO {GetString(node)}");
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        SetString(node, GetString(node.ExpressionValueNode));
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallExpressionNode node)
    {
        SetString(node, GetString(node.FunctionNode));
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        var args = string.Join(", ", node.Arguments.Select(GetString));
        SetString(node, $"{node.FunctionName}({args})");
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        SetString(node, GetString(node.FunctionNode));
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override void Visit(SelectColumnsListNode node)
    {
        SetString(node, string.Join(", ", node.ColumnsNodes.Select(GetString)));
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistAll node)
    {
        SetString(node, "*");
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        SetString(node, GetString(node));
    }

    /// <inheritdoc />
    public override void Visit(SelectFetchNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(SelectGroupByNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(SelectHavingNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(SelectOffsetNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(SelectOrderByNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(SelectOrderBySpecificationNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT {GetString(node.ColumnsListNode)}");
        if (node.TableExpressionNode != null)
        {
            sb.Append(Space);
            sb.Append(GetString(node.TableExpressionNode));
        }
        SetString(node, sb.ToString());
    }

    /// <inheritdoc />
    public override void Visit(SelectSearchConditionNode node)
    {
        SetString(node, $"WHERE {GetString(node.ExpressionNode)}");
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        SetString(node, GetString(node.QueryNode));
    }

    /// <inheritdoc />
    public override void Visit(SelectTableExpressionNode node)
    {
        var sb = new StringBuilder();
        sb.Append(GetString(node.TablesNode));
        if (node.GroupByNode != null)
        {
            sb.Append(Space);
            sb.Append(GetString(node.GroupByNode));
        }
        if (node.HavingNode != null)
        {
            sb.Append(Space);
            sb.Append(GetString(node.HavingNode));
        }
        if (node.SearchConditionNode != null)
        {
            sb.Append(Space);
            sb.Append(GetString(node.SearchConditionNode));
        }
        SetString(node, sb.ToString());
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        var value = GetString(node.TableFunctionNode);
        if (!string.IsNullOrEmpty(node.Alias))
        {
            value += " AS " + node.Alias;
        }
        SetString(node, value);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableReferenceListNode node)
    {
        SetString(node, string.Join(", ", node.TableFunctionsNodes.Select(GetString)));
    }

    #endregion

    private static void SetString(IAstNode node, string value) => node.SetAttribute(AstAttributeKeys.StringKey, value);

    public static string GetString(IAstNode node) => node.GetAttribute<string>(AstAttributeKeys.StringKey) ?? string.Empty;

    public static string GetStringWithParens(IAstNode node) => string.Concat("(", GetString(node), ")");

    private static string GetOperationString(VariantValue.Operation operation) => operation switch
    {
        VariantValue.Operation.Add => "+",
        VariantValue.Operation.Subtract => "-",
        VariantValue.Operation.Multiple => "*",
        VariantValue.Operation.Divide => "/",

        VariantValue.Operation.Equals => "=",
        VariantValue.Operation.NotEquals => "!=",
        VariantValue.Operation.Greater => ">",
        VariantValue.Operation.GreaterOrEquals => ">=",
        VariantValue.Operation.Less => "<",
        VariantValue.Operation.LessOrEquals => "<=",
        VariantValue.Operation.Between => "BETWEEN",
        VariantValue.Operation.IsNull => "IS NULL",
        VariantValue.Operation.IsNotNull => "IS NOT NULL",
        VariantValue.Operation.And => "AND",
        VariantValue.Operation.Or => "OR",
        VariantValue.Operation.Not => "-",
        VariantValue.Operation.BetweenAnd => string.Empty,
        VariantValue.Operation.Concat => "||",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };
}
