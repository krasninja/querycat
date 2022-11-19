using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    #region Statement

    /// <inheritdoc />
    public override IAstNode VisitStatementEcho(QueryCatParser.StatementEchoContext context)
        => (ExpressionStatementNode)Visit(context.echoStatement());

    #endregion

    /// <inheritdoc />
    public override IAstNode VisitEchoStatement(QueryCatParser.EchoStatementContext context)
        => new ExpressionStatementNode(this.Visit<ExpressionNode>(context.expression()));
}
