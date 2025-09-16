using System.Globalization;
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
    public override async ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        var traversal = new AstTraversal(this);
        await traversal.PostOrderAsync(node, cancellationToken);
    }

    #region General

    /// <inheritdoc />
    public override ValueTask VisitAsync(BetweenExpressionNode node, CancellationToken cancellationToken)
    {
        Set(node, string.Join(Space,
            GetStringWithParens(node.Expression),
            GetOperationString(VariantValue.Operation.Between),
            GetStringWithParens(node.Left),
            GetOperationString(VariantValue.Operation.And),
            GetStringWithParens(node.Right)));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        Set(node, string.Join(Space,
            GetStringWithParens(node.LeftNode),
            GetOperationString(node.Operation),
            GetStringWithParens(node.RightNode)));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(ExpressionStatementNode node, CancellationToken cancellationToken)
    {
        Copy(node, node.ExpressionNode);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        Set(node, node.Name);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InExpressionValuesNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        string value;
        if (node.Value.Type == DataType.String)
        {
            value = StringUtils.Quote(node.Value.AsStringUnsafe, quote: "'", force: true).ToString();
        }
        else
        {
            value = node.Value.ToString(CultureInfo.InvariantCulture);
        }
        Set(node, value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(ProgramNode node, CancellationToken cancellationToken)
    {
        Copy(node, node.Body);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(ProgramBodyNode node, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        foreach (var statementNode in node.Statements)
        {
            sb.Append(Get(statementNode));
            sb.Append(';');
            sb.Append('\n');
        }
        Set(node, sb.ToString());
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(TernaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(TypeNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(UnaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        Set(node, string.Join(Space,
            GetOperationString(node.Operation),
            Get(node.RightNode)));
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Declare

    /// <inheritdoc />
    public override ValueTask VisitAsync(DeclareNode node, CancellationToken cancellationToken)
    {
        var str = $"DECLARE {node.Name}";
        if (node.ValueNode != null)
        {
            str += " := " + Get(node.ValueNode);
        }
        Set(node, str);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(DeclareStatementNode node, CancellationToken cancellationToken)
    {
        Copy(node, node.RootNode);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallArgumentNode node, CancellationToken cancellationToken)
    {
        Set(node, Get(node.ExpressionValueNode));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallExpressionNode node, CancellationToken cancellationToken)
    {
        Set(node, Get(node.FunctionNode));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        var args = string.Join(", ", node.Arguments.Select(Get));
        Set(node, $"{node.FunctionName}({args})");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        Set(node, Get(node.FunctionNode));
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsListNode node, CancellationToken cancellationToken)
    {
        Set(node, string.Join(", ", node.ColumnsNodes.Select(Get)));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistAll node, CancellationToken cancellationToken)
    {
        Set(node, "*");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        Copy(node, node.ExpressionNode);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectFetchNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectGroupByNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectHavingNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOffsetNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOrderByNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOrderBySpecificationNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT {Get(node.ColumnsListNode)}");
        if (node.TableExpressionNode != null)
        {
            sb.Append(Get(node.TableExpressionNode));
        }
        Set(node, sb.ToString());
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectSearchConditionNode node, CancellationToken cancellationToken)
    {
        Set(node, $" WHERE {Get(node.ExpressionNode)}");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectStatementNode node, CancellationToken cancellationToken)
    {
        Set(node, Get(node.QueryNode));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableNode node, CancellationToken cancellationToken)
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
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        var value = Get(node.TableFunctionNode);
        if (!string.IsNullOrEmpty(node.Alias))
        {
            value += " AS " + node.Alias;
        }
        Set(node, value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableReferenceListNode node, CancellationToken cancellationToken)
    {
        Set(node, string.Join(", ", node.TableFunctionsNodes.Select(Get)));
        return ValueTask.CompletedTask;
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
        DataType.Null => VariantValue.NullValueString,
        DataType.Void => VariantValue.VoidValueString,
        DataType.Dynamic => VariantValue.VoidValueString,
        DataType.Integer => "INTEGER",
        DataType.String => "STRING",
        DataType.Float => "FLOAT",
        DataType.Timestamp => "TIMESTAMP",
        DataType.Boolean => "BOOL",
        DataType.Numeric => "NUMERIC",
        DataType.Interval => "INTERVAL",
        DataType.Blob => "BLOB",
        DataType.Object => "OBJECT",
        DataType.Array => "ARRAY",
        DataType.Map => "MAP",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, Resources.Errors.InvalidArgumentType),
    };

    /// <summary>
    /// Dump as a string.
    /// </summary>
    /// <param name="node">Node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query expression.</returns>
    public async Task<string> DumpAsync(IAstNode node, CancellationToken cancellationToken = default)
    {
        await this.RunAsync(node, cancellationToken);
        return Get(node);
    }
}
