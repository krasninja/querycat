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
    public override async ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        _columns.Clear();
        await base.RunAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (node.IsCurrentSpecialIdentifier)
        {
            return ValueTask.CompletedTask;
        }
        if (!string.IsNullOrEmpty(node.TableSourceName)
            && node.TableSourceName != _sourceName)
        {
            return ValueTask.CompletedTask;
        }
        // Prevent duplication.
        if (_columns.Any(c => c.Name.Equals(node.TableFieldName)))
        {
            return ValueTask.CompletedTask;
        }

        _columns.Add(new Column(node.TableFieldName, _sourceName, DataType.Void));
        return ValueTask.CompletedTask;
    }
}
