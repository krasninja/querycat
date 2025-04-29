using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Continue;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitStatementContinue(QueryCatParser.StatementContinueContext context)
        => new ContinueStatementNode(this.Visit<ContinueNode>(context.continueStatement()));

    /// <inheritdoc />
    public override IAstNode VisitContinueStatement(QueryCatParser.ContinueStatementContext context)
        => new ContinueNode();
}
