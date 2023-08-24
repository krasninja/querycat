using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.Update;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitUpdateStatement(QueryCatParser.UpdateStatementContext context)
        => new UpdateStatementNode(new UpdateNode(
                this.Visit<ExpressionNode>(context.updateSource()),
                this.Visit<UpdateSetNode>(context.updateSetClause())
            )
            {
                SearchConditionNode = this.VisitMaybe<SelectSearchConditionNode>(context.selectSearchCondition())
            });

    /// <inheritdoc />
    public override IAstNode VisitUpdateSetClause(QueryCatParser.UpdateSetClauseContext context)
        => new UpdateSetNode(
            this.Visit<IdentifierExpressionNode>(context.identifierChain()),
            this.Visit<ExpressionNode>(context.target));

    #region Source

    /// <inheritdoc />
    public override IAstNode VisitUpdateNoFormat(QueryCatParser.UpdateNoFormatContext context)
        => this.Visit<FunctionCallNode>(context.functionCall());

    /// <inheritdoc />
    public override IAstNode VisitUpdateWithFormat(QueryCatParser.UpdateWithFormatContext context)
    {
        var readFunction = new FunctionCallNode("read");
        var uri = GetUnwrappedText(context.uri);
        readFunction.Arguments.Add(new FunctionCallArgumentNode("uri",
            new LiteralNode(new VariantValue(uri))));
        if (context.functionCall() != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(context.functionCall());
            readFunction.Arguments.Add(new FunctionCallArgumentNode("fmt", formatterFunctionCallNode));
        }
        return new FunctionCallNode(readFunction);
    }

    /// <inheritdoc />
    public override IAstNode VisitUpdateFromVariable(QueryCatParser.UpdateFromVariableContext context)
        => this.Visit<IdentifierExpressionNode>(context.identifierChain());

    #endregion
}
