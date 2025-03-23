using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitInsertStatement(QueryCatParser.InsertStatementContext context)
        => new InsertStatementNode(
            new InsertNode(
                this.Visit<ExpressionNode>(context.insertToSource()),
                this.Visit<SelectQueryNode>(context.insertFromSource())
            )
            {
                ColumnsNode = this.VisitMaybe<InsertColumnsListNode>(context.insertColumnsList()),
            });

    #region From

    /// <inheritdoc />
    public override IAstNode VisitInsertSourceQuery(QueryCatParser.InsertSourceQueryContext context)
        => this.Visit<SelectQueryNode>(context.selectQueryExpression());

    /// <inheritdoc />
    public override IAstNode VisitInsertSourceTable(QueryCatParser.InsertSourceTableContext context)
    {
        return new SelectQuerySpecificationNode(new SelectColumnsListNode(new SelectColumnsSublistAll()))
        {
            TableExpressionNode = new SelectTableNode(
                new SelectTableReferenceListNode(this.Visit<SelectTableValuesNode>(context.selectTableValues()))
            )
        };
    }

    #endregion

    #region To

    /// <inheritdoc />
    public override IAstNode VisitInsertColumnsList(QueryCatParser.InsertColumnsListContext context)
        => new InsertColumnsListNode(
            this.Visit<IdentifierExpressionNode>(context.identifier()).Select(n => n.Name));

    /// <inheritdoc />
    public override IAstNode VisitInsertNoFormat(QueryCatParser.InsertNoFormatContext context)
        => new SelectTableFunctionNode(this.Visit<FunctionCallNode>(context.functionCall()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName,
        };

    /// <inheritdoc />
    public override IAstNode VisitInsertWithFormat(QueryCatParser.InsertWithFormatContext context)
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
    public override IAstNode VisitInsertFromVariable(QueryCatParser.InsertFromVariableContext context)
    {
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return new SelectIdentifierExpressionNode(this.Visit<IdentifierExpressionNode>(context.identifier()), alias);
    }

    #endregion
}
