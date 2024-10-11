using System.Text;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Variable identifier.
/// </summary>
internal class IdentifierExpressionNode : ExpressionNode
{
    public const string CurrentSymbol = "@";

    /// <summary>
    /// Identifier name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The field name for data (SELECT, UPDATE, etc.) commands.
    /// </summary>
    public string TableFieldName { get; }

    /// <summary>
    /// The source/table name for data (SELECT, UPDATE, etc.) commands. Should be empty for local variables.
    /// </summary>
    public string TableSourceName { get; internal set; } = string.Empty;

    /// <summary>
    /// Full name.
    /// </summary>
    public string FullName => Name + DumpNameWithSelector();

    /// <summary>
    /// Full name in the SELECT clause.
    /// </summary>
    public string TableFullName
        => !string.IsNullOrEmpty(TableSourceName) ? $"{TableSourceName}.{TableFieldName}" : TableFieldName;

    /// <inheritdoc />
    public override string Code => "id";

    /// <summary>
    /// Selectors.
    /// </summary>
    public IdentifierSelectorNode[] SelectorNodes { get; } = [];

    public bool HasSelectors => SelectorNodes.Length > 0;

    /// <summary>
    /// The special identifier that can be used in objects selector.
    /// </summary>
    /// <remarks>
    /// RFC: https://www.rfc-editor.org/rfc/rfc9535.html#name-summary.
    /// </remarks>
    public bool IsCurrentSpecialIdentifier => Name == CurrentSymbol;

    /// <inheritdoc />
    public IdentifierExpressionNode(string name, List<IdentifierSelectorNode>? selectorNodes = null)
    {
        Name = StringUtils.GetUnwrappedText(name);
        TableFieldName = Name;

        if (selectorNodes != null)
        {
            SelectorNodes = selectorNodes.ToArray();
            if (SelectorNodes.Length == 1 && SelectorNodes[0] is IdentifierPropertySelectorNode propertySelectorNode)
            {
                TableFieldName = propertySelectorNode.PropertyName;
                TableSourceName = Name;
            }
        }
    }

    /// <inheritdoc />
    public IdentifierExpressionNode(string name, string sourceName) : this(name)
    {
        TableSourceName = StringUtils.GetUnwrappedText(sourceName);
    }

    public IdentifierExpressionNode(IdentifierExpressionNode node) : this(node.Name)
    {
        SelectorNodes = node.SelectorNodes.Select(s => (IdentifierSelectorNode)s.Clone()).ToArray();
        TableSourceName = node.TableSourceName;
        TableFieldName = node.TableFieldName;
        Name = node.Name;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var selectorNode in SelectorNodes)
        {
            yield return selectorNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new IdentifierExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    public string DumpNameWithSelector()
    {
        var sb = new StringBuilder();
        foreach (var selectorNode in SelectorNodes)
        {
            sb.Append(selectorNode);
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => $"id: {FullName}";
}
