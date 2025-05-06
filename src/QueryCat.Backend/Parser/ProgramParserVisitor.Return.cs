using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Return;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitStatementReturn(QueryCatParser.StatementReturnContext context)
        => new ReturnStatementNode(this.Visit<ReturnNode>(context.returnStatement()));

    /// <inheritdoc />
    public override IAstNode VisitReturnStatement(QueryCatParser.ReturnStatementContext context)
        => new ReturnNode(this.VisitMaybe<ExpressionNode>(context.expression()));
}
