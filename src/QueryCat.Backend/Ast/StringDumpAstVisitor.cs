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
internal sealed class StringDumpAstVisitor(StringBuilder output) : DelegateVisitor
{
    /// <inheritdoc />
    public override ValueTask OnVisitAsync(IAstNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node);
        return ValueTask.CompletedTask;
    }

    private void PrettyPrintNode(IAstNode node,
        params (string Key, object? Value)[] @params)
    {
        var ident = (AstTraversal.GetCurrentStack().Count() - 1) * 3;
        output.Append(new string(' ', ident));
        output.Append($"- <{node.Code}>:");

        var paramsString = string.Join(", ",
            @params
                .Where(p => p.Value != null
                    && IsSimpleType(p.Value.GetType())
                    && !ReferenceEquals(p.Value, string.Empty) )
                .Select(p => $"{p.Key}: {p.Value}"));
        if (!string.IsNullOrEmpty(paramsString))
        {
            output.Append($" ({paramsString})");
        }
        output.AppendLine();

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
                    output.Append(new string(' ', ident));
                    output.AppendLine($"  {attribute.Key}: {value}");
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
    public override ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("op", node.Operation));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("name", node.Name));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("value", node.Value.ToString(CultureInfo.InvariantCulture)));
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("name", node.FunctionName));
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistAll node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("alias", "*"));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("alias", node.Alias));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistNode node, CancellationToken cancellationToken)
    {
        PrettyPrintNode(node, ("alias", node.Alias));
        return ValueTask.CompletedTask;
    }

    #endregion

    /*
     * The thread can be helpful:
     * https://stackoverflow.com/questions/61944125/traversing-a-graph-of-unknown-object-types-and-mutating-some-object-properties
     */

    private static readonly Type[] _simpleTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    ];

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || Array.IndexOf(_simpleTypes, type) > -1
            || type.IsEnum
            || Convert.GetTypeCode(type) != TypeCode.Object
            || (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                && IsSimpleType(type.GetGenericArguments()[0]));
    }
}
