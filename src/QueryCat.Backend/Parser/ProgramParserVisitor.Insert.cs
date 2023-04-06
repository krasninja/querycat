using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitInsertStatement(QueryCatParser.InsertStatementContext context)
        => new InsertStatementNode(
            new InsertNode(
                this.Visit<FunctionCallNode>(context.insertToSource()),
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
        return new SelectQuerySpecificationNode(new SelectColumnsListNode(SelectColumnsSublistAll.Instance))
        {
            TableExpressionNode = new SelectTableExpressionNode(
                new SelectTableReferenceListNode(this.Visit<SelectTableNode>(context.selectTable()))
            )
        };
    }

    #endregion

    #region To

    /// <inheritdoc />
    public override IAstNode VisitInsertColumnsList(QueryCatParser.InsertColumnsListContext context)
        => new InsertColumnsListNode(
            this.Visit<IdentifierExpressionNode>(context.identifierChain()).Select(n => n.Name));

    /// <inheritdoc />
    public override IAstNode VisitInsertNoFormat(QueryCatParser.InsertNoFormatContext context)
        => this.Visit<FunctionCallNode>(context.functionCall());

    /// <inheritdoc />
    public override IAstNode VisitInsertWithFormat(QueryCatParser.InsertWithFormatContext context)
    {
        var readFunction = new FunctionCallNode("write");
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

    #endregion
}
