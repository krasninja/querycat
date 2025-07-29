using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Open;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitStatementOpen(QueryCatParser.StatementOpenContext context)
        => new OpenStatementNode(this.Visit<OpenNode>(context.openStatement()));

    /// <inheritdoc />
    public override IAstNode VisitOpenStatement(QueryCatParser.OpenStatementContext context)
        => new OpenNode(this.Visit<ExpressionNode>(context.source));
}
