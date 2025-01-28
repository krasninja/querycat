using System.Globalization;
using System.Text;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast;

/// <summary>
/// The visitor allows to dump AST into a string for debug
/// and better graphical representation.
/// </summary>
internal sealed class StringDumpAstVisitor : DelegateVisitor
{
    private readonly StringBuilder _output;
    private readonly AstTraversal _astTraversal;

    public StringDumpAstVisitor(StringBuilder output)
    {
        _output = output;
        _astTraversal = new AstTraversal(this);
    }

    /// <inheritdoc />
    public override void OnVisit(IAstNode node)
    {
        PrettyPrintNode(node);
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        _astTraversal.PreOrder(node);
    }

    private void PrettyPrintNode(IAstNode node,
        params (string Key, object? Value)[] @params)
    {
        var ident = (_astTraversal.GetCurrentStack().Count() - 1) * 3;
        _output.Append(new string(' ', ident));
        _output.Append($"- <{node.Code}>:");

        var paramsString = string.Join(", ",
            @params
                .Where(p => p.Value != null
                    && IsSimpleType(p.Value.GetType())
                    && !ReferenceEquals(p.Value, string.Empty) )
                .Select(p => $"{p.Key}: {p.Value}"));
        if (!string.IsNullOrEmpty(paramsString))
        {
            _output.Append($" ({paramsString})");
        }
        _output.AppendLine();

        PrettyPrintAttributes(ident, node);
    }

    private void PrettyPrintAttributes(int ident, IAstNode node)
    {
        if (node is AstNode astNode)
        {
            var attributes = astNode.GetAttributes();
            if (attributes.Any())
            {
                foreach (var attribute in attributes)
                {
                    var value = FormatValue(attribute.Value);
                    _output.Append(new string(' ', ident));
                    _output.AppendLine($"  {attribute.Key}: {value}");
                }
            }
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is IFuncUnit funcUnit)
        {
            return funcUnit.ToString() ?? nameof(IFuncUnit);
        }
        if (value is Func<VariantValue>)
        {
            return nameof(Func<VariantValue>) + "`0";
        }
        return value?.ToString() ?? string.Empty;
    }

    #region General

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        PrettyPrintNode(node, ("op", node.Operation));
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        PrettyPrintNode(node, ("name", node.Name));
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        PrettyPrintNode(node, ("value", node.Value.ToString(CultureInfo.InvariantCulture)));
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        PrettyPrintNode(node, ("name", node.FunctionName));
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistAll node)
    {
        PrettyPrintNode(node, ("alias", "*"));
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        PrettyPrintNode(node, ("alias", node.Alias));
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistNode node)
    {
        PrettyPrintNode(node, ("alias", node.Alias));
    }

    #endregion

    /*
     * The thread can be helpful:
     * https://stackoverflow.com/questions/61944125/traversing-a-graph-of-unknown-object-types-and-mutating-some-object-properties
     */

    private static readonly Type[] SimpleTypes =
    {
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    };

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || Array.IndexOf(SimpleTypes, type) > -1
            || type.IsEnum
            || Convert.GetTypeCode(type) != TypeCode.Object
            || (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                && IsSimpleType(type.GetGenericArguments()[0]));
    }
}
