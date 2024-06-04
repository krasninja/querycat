using System.Text;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Serialize the AST representation of query into a text string.
/// </summary>
internal class QueryAstVisitor : AstVisitor
{
    private const char Space = ' ';

    private readonly Dictionary<int, string> _nodeIdStringMap = new(capacity: 32);

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
        Set(node, string.Join(Space,
            GetStringWithParens(node.Expression),
            GetOperationString(VariantValue.Operation.Between),
            GetStringWithParens(node.Left),
            GetOperationString(VariantValue.Operation.And),
            GetStringWithParens(node.Right)));
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        Set(node, string.Join(Space,
            GetStringWithParens(node.LeftNode),
            GetOperationString(node.Operation),
            GetStringWithParens(node.RightNode)));
    }

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
    {
        Copy(node, node.ExpressionNode);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        Set(node, node.Name);
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
        var type = node.Value.GetInternalType();
        string value;
        if (type == DataType.String)
        {
            value = StringUtils.Quote(node.Value.AsStringUnsafe, quote: "'", force: true).ToString();
        }
        else
        {
            value = node.Value.ToString();
        }
        Set(node, value);
    }

    /// <inheritdoc />
    public override void Visit(ProgramNode node)
    {
        var sb = new StringBuilder();
        foreach (var statementNode in node.Statements)
        {
            sb.Append(Get(statementNode));
            sb.Append(';');
            sb.Append('\n');
        }
        Set(node, sb.ToString());
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
        Set(node, string.Join(Space,
            GetOperationString(node.Operation),
            Get(node.RightNode)));
    }

    #endregion

    #region Declare

    /// <inheritdoc />
    public override void Visit(DeclareNode node)
    {
        var str = $"DECLARE {node.Name} {GetTypeString(node.Type)}";
        if (node.ValueNode != null)
        {
            str += " := " + Get(node.ValueNode);
        }
        Set(node, str);
    }

    /// <inheritdoc />
    public override void Visit(DeclareStatementNode node)
    {
        Copy(node, node.RootNode);
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        Set(node, Get(node.ExpressionValueNode));
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallExpressionNode node)
    {
        Set(node, Get(node.FunctionNode));
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        var args = string.Join(", ", node.Arguments.Select(Get));
        Set(node, $"{node.FunctionName}({args})");
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        Set(node, Get(node.FunctionNode));
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override void Visit(SelectColumnsListNode node)
    {
        Set(node, string.Join(", ", node.ColumnsNodes.Select(Get)));
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistAll node)
    {
        Set(node, "*");
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        Copy(node, node.ExpressionNode);
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
        sb.Append($"SELECT {Get(node.ColumnsListNode)}");
        if (node.TableExpressionNode != null)
        {
            sb.Append(Get(node.TableExpressionNode));
        }
        Set(node, sb.ToString());
    }

    /// <inheritdoc />
    public override void Visit(SelectSearchConditionNode node)
    {
        Set(node, $" WHERE {Get(node.ExpressionNode)}");
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        Set(node, Get(node.QueryNode));
    }

    /// <inheritdoc />
    public override void Visit(SelectTableNode node)
    {
        var sb = new StringBuilder();
        sb.Append(" FROM ");
        sb.Append(Get(node.TablesNode));
        if (node.GroupByNode != null)
        {
            sb.Append(Get(node.GroupByNode));
        }
        if (node.HavingNode != null)
        {
            sb.Append(Get(node.HavingNode));
        }
        if (node.SearchConditionNode != null)
        {
            sb.Append(Get(node.SearchConditionNode));
        }
        Set(node, sb.ToString());
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        var value = Get(node.TableFunctionNode);
        if (!string.IsNullOrEmpty(node.Alias))
        {
            value += " AS " + node.Alias;
        }
        Set(node, value);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableReferenceListNode node)
    {
        Set(node, string.Join(", ", node.TableFunctionsNodes.Select(Get)));
    }

    #endregion

    private void Set(IAstNode node, string value) => _nodeIdStringMap[node.Id] = value;

    internal string Get(IAstNode node) => _nodeIdStringMap.GetValueOrDefault(node.Id, string.Empty);

    private string Copy(IAstNode node, IAstNode fromNode) => _nodeIdStringMap[node.Id] = Get(fromNode);

    private string GetStringWithParens(IAstNode node) => string.Concat("(", Get(node), ")");

    private static string GetOperationString(VariantValue.Operation operation) => operation switch
    {
        VariantValue.Operation.Add => "+",
        VariantValue.Operation.Subtract => "-",
        VariantValue.Operation.Multiple => "*",
        VariantValue.Operation.Divide => "/",
        VariantValue.Operation.Modulo => "%",
        VariantValue.Operation.LeftShift => "<<",
        VariantValue.Operation.RightShift => ">>",

        VariantValue.Operation.Equals => "=",
        VariantValue.Operation.NotEquals => "!=",
        VariantValue.Operation.Greater => ">",
        VariantValue.Operation.GreaterOrEquals => ">=",
        VariantValue.Operation.Less => "<",
        VariantValue.Operation.LessOrEquals => "<=",
        VariantValue.Operation.Between => "BETWEEN",
        VariantValue.Operation.BetweenAnd => string.Empty,
        VariantValue.Operation.IsNull => "IS NULL",
        VariantValue.Operation.IsNotNull => "IS NOT NULL",
        VariantValue.Operation.Like => "LIKE",
        VariantValue.Operation.NotLike => "NOT LIKE",
        VariantValue.Operation.Similar => "SIMILAR",
        VariantValue.Operation.NotSimilar => "NOT SIMILAR",
        VariantValue.Operation.In => "IN",

        VariantValue.Operation.And => "AND",
        VariantValue.Operation.Or => "OR",
        VariantValue.Operation.Not => "NOT",

        VariantValue.Operation.Concat => "||",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };

    private static string GetTypeString(DataType type) => type switch
    {
        DataType.Void => VariantValue.VoidValueString,
        DataType.Null => VariantValue.NullValueString,
        DataType.Integer => "INTEGER",
        DataType.String => "STRING",
        DataType.Float => "FLOAT",
        DataType.Timestamp => "TIMESTAMP",
        DataType.Boolean => "BOOL",
        DataType.Numeric => "NUMERIC",
        DataType.Interval => "INTERVAL",
        DataType.Blob => "BLOB",
        DataType.Object => "OBJECT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, Resources.Errors.InvalidArgumentType),
    };

    /// <summary>
    /// Dump as a string.
    /// </summary>
    /// <param name="node">Node.</param>
    /// <returns>Query expression.</returns>
    public string Dump(IAstNode node)
    {
        this.Run(node);
        return Get(node);
    }
}
