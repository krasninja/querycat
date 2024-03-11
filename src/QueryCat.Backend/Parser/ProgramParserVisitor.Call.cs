using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    #region Call

    /// <inheritdoc />
    public override IAstNode VisitStatementCall(QueryCatParser.StatementCallContext context)
        => new CallFunctionStatementNode(
            this.Visit<CallFunctionNode>(context.callStatement()));

    /// <inheritdoc />
    public override IAstNode VisitCallStatement(QueryCatParser.CallStatementContext context)
        => new CallFunctionNode(
            this.Visit<FunctionCallNode>(context.functionCall()));

    #endregion
}
