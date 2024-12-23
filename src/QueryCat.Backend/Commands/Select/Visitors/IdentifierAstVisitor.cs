using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor is to get all identifier columns.
/// </summary>
internal sealed class IdentifierAstVisitor : AstVisitor
{
    private readonly string _sourceName;
    private readonly List<Column> _columns = new();

    public IReadOnlyList<Column> Columns => _columns;

    public IdentifierAstVisitor(string sourceName)
    {
        _sourceName = sourceName;
        AstTraversal.TypesToIgnore.Add(typeof(SelectColumnsSublistAll));
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        _columns.Clear();
        base.Run(node);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        if (node.IsCurrentSpecialIdentifier)
        {
            return;
        }
        if (!string.IsNullOrEmpty(node.TableSourceName)
            && node.TableSourceName != _sourceName)
        {
            return;
        }
        // Prevent duplication.
        if (_columns.Any(c => c.Name.Equals(node.TableFieldName)))
        {
            return;
        }

        _columns.Add(new Column(node.TableFieldName, _sourceName, DataType.Void));
    }
}
