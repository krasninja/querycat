using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitStatementDeclareVariable(QueryCatParser.StatementDeclareVariableContext context)
        => new DeclareStatementNode(this.Visit<DeclareNode>(context.declareVariable()));

    /// <inheritdoc />
    public override IAstNode VisitStatementSetVariable(QueryCatParser.StatementSetVariableContext context)
        => new SetStatementNode(this.Visit<SetNode>(context.setVariable()));

    /// <inheritdoc />
    public override IAstNode VisitDeclareVariable(QueryCatParser.DeclareVariableContext context)
        => new DeclareNode(
            GetUnwrappedText(context.identifierSimple()),
            this.Visit<TypeNode>(context.type()).Type,
            context.statement() != null ? this.Visit<StatementNode>(context.statement()) : null
        );

    /// <inheritdoc />
    public override IAstNode VisitSetVariable(QueryCatParser.SetVariableContext context)
        => new SetNode(
            GetUnwrappedText(context.identifierSimple()),
            this.Visit<StatementNode>(context.statement())
        );
}
