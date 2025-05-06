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
            this.Visit<IdentifierExpressionNode>(context.identifier()),
            this.Visit<ExpressionNode>(context.target));

    #region Source

    /// <inheritdoc />
    public override IAstNode VisitUpdateNoFormat(QueryCatParser.UpdateNoFormatContext context)
        => new SelectTableFunctionNode(this.Visit<FunctionCallNode>(context.functionCall()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName,
        };

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
        return new SelectTableFunctionNode(readFunction)
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName,
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitUpdateFromVariable(QueryCatParser.UpdateFromVariableContext context)
    {
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return new SelectIdentifierExpressionNode(this.Visit<IdentifierExpressionNode>(context.identifier()), alias);
    }

    #endregion
}
