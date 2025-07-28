using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Delete;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitDeleteStatement(QueryCatParser.DeleteStatementContext context)
        => new DeleteStatementNode(
            new DeleteNode(
                this.Visit<ExpressionNode>(context.deleteFromSource()),
                this.VisitMaybe<SelectSearchConditionNode>(context.selectSearchCondition()))
            );

    #region From

    /// <inheritdoc />
    public override IAstNode VisitDeleteNoFormat(QueryCatParser.DeleteNoFormatContext context)
        => new SelectTableFunctionNode(this.Visit<FunctionCallNode>(context.functionCall()))
        {
            Alias = GetContextAlias(context.selectAlias),
        };

    /// <inheritdoc />
    public override IAstNode VisitDeleteWithFormat(QueryCatParser.DeleteWithFormatContext context)
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
            Alias = GetContextAlias(context.selectAlias),
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitDeleteFromVariable(QueryCatParser.DeleteFromVariableContext context)
    {
        var alias = GetContextAlias(context.selectAlias);
        return new SelectIdentifierExpressionNode(this.Visit<IdentifierExpressionNode>(context.identifier()), alias);
    }

    #endregion
}
