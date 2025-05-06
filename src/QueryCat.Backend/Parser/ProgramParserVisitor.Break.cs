using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Break;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitStatementBreak(QueryCatParser.StatementBreakContext context)
        => new BreakStatementNode(this.Visit<BreakNode>(context.breakStatement()));

    /// <inheritdoc />
    public override IAstNode VisitBreakStatement(QueryCatParser.BreakStatementContext context)
        => new BreakNode();
}
