using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.For;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitForStatement(QueryCatParser.ForStatementContext context)
        => new ForStatementNode(
            new ForNode(
                GetUnwrappedText(context.target),
                this.Visit<ExpressionNode>(context.query),
                this.Visit<ProgramBodyNode>(context.programBody())
            )
        );
}
