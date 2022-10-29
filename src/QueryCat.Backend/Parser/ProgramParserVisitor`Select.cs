using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    #region Statement

    /// <inheritdoc />
    public override IAstNode VisitStatementSelectExpression(QueryCatParser.StatementSelectExpressionContext context)
        => new SelectStatementNode((SelectQueryExpressionBodyNode)Visit(context.selectStatement()));

    #endregion

    #region Select

    /// <inheritdoc />
    public override IAstNode VisitSelectStatement(QueryCatParser.SelectStatementContext context)
        => this.Visit<SelectQueryExpressionBodyNode>(context.selectQueryExpression());

    /// <inheritdoc />
    public override IAstNode VisitSelectAlias(QueryCatParser.SelectAliasContext context)
        => new SelectAliasNode(GetUnwrappedText(context.name.Text));

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpression(QueryCatParser.SelectQueryExpressionContext context)
        => new SelectQueryExpressionBodyNode(
            this.Visit<SelectQuerySpecificationNode>(context.selectQuery()).ToArray())
        {
            OrderBy = this.VisitMaybe<SelectOrderByNode>(context.selectOrderByClause()),
            Offset = this.VisitMaybe<SelectOffsetNode>(context.selectOffsetClause()),
            Fetch = this.VisitMaybe<SelectFetchNode>(context.selectFetchFirstClause()),
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryFull(QueryCatParser.SelectQueryFullContext context)
        => new SelectQuerySpecificationNode(this.Visit<SelectColumnsListNode>(context.selectList()))
        {
            QuantifierNode = this.Visit(context.selectSetQuantifier(), new SelectSetQuantifierNode(false)),
            TableExpression = this.VisitMaybe<SelectTableExpressionNode>(context.selectFromClause()),
            Target = this.VisitMaybe<FunctionCallNode>(context.selectTarget()),
            OrderBy = this.VisitMaybe<SelectOrderByNode>(context.selectOrderByClause()),
            Offset = this.VisitMaybe<SelectOffsetNode>(context.selectOffsetClause()),
            Fetch = this.VisitMaybe<SelectFetchNode>(context.selectFetchFirstClause()),
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectQuerySingle(QueryCatParser.SelectQuerySingleContext context)
    {
        var selectColumnsSublistNodes = this.Visit<SelectColumnsSublistNode>(context.selectSublist()).ToList();
        return new SelectQuerySpecificationNode(new SelectColumnsListNode(selectColumnsSublistNodes))
        {
            Target = this.VisitMaybe<FunctionCallNode>(context.selectTarget())
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectList(QueryCatParser.SelectListContext context)
        => new SelectColumnsListNode(this.Visit<SelectColumnsSublistNode>(context.selectSublist()).ToList());

    /// <inheritdoc />
    public override IAstNode VisitSelectSetQuantifier(QueryCatParser.SelectSetQuantifierContext context)
        => new SelectSetQuantifierNode(context.DISTINCT() != null);

    #endregion

    #region Columns

    // 7.16 <query specification>

    /// <inheritdoc />
    public override IAstNode VisitSelectSublistAll(QueryCatParser.SelectSublistAllContext context)
        => new SelectColumnsSublistAll();

    /// <inheritdoc />
    public override IAstNode VisitSelectSublistExpression(QueryCatParser.SelectSublistExpressionContext context)
        => new SelectColumnsSublistExpressionNode((ExpressionNode)Visit(context.expression()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectSublistIdentifier(QueryCatParser.SelectSublistIdentifierContext context)
    {
        var idNode = this.Visit<IdentifierExpressionNode>(context.identifierChain());
        return new SelectColumnsSublistNameNode(
            columnName: idNode.Name,
            sourceName: idNode.SourceName)
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionSelect(QueryCatParser.ExpressionSelectContext context)
        => this.Visit<SelectQueryExpressionBodyNode>(context.selectQueryExpression());

    /// <inheritdoc />
    public override IAstNode VisitExpressionExists(QueryCatParser.ExpressionExistsContext context)
        => new SelectExistsExpressionNode(this.Visit<SelectQueryExpressionBodyNode>(context.selectQueryExpression()));

    #endregion

    #region Into

    /// <inheritdoc />
    public override IAstNode VisitSelectTarget(QueryCatParser.SelectTargetContext context)
        => this.Visit<FunctionCallNode>(context.functionCall());

    #endregion

    #region From

    // 7.5 <from clause>
    // 7.6 <table reference>

    /// <inheritdoc />
    public override IAstNode VisitSelectFromClause(QueryCatParser.SelectFromClauseContext context)
        => new SelectTableExpressionNode(this.Visit<SelectTableReferenceListNode>(context.selectTableReferenceList()))
        {
            SearchConditionNode = this.VisitMaybe<SelectSearchConditionNode>(context.selectSearchCondition()),
            GroupByNode = this.VisitMaybe<SelectGroupByNode>(context.selectGroupBy()),
            HavingNode = this.VisitMaybe<SelectHavingNode>(context.selectHaving()),
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectTableReferenceList(QueryCatParser.SelectTableReferenceListContext context)
        => new SelectTableReferenceListNode(this.Visit<ExpressionNode>(context.selectTableReference())
            .ToList());

    /// <inheritdoc />
    public override IAstNode VisitSelectTableReferenceNoFormat(QueryCatParser.SelectTableReferenceNoFormatContext context)
        => new SelectTableFunctionNode(this.Visit<FunctionCallNode>(context.functionCall()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectTableReferenceWithFormat(QueryCatParser.SelectTableReferenceWithFormatContext context)
    {
        var readFunction = new FunctionCallNode("read");
        var uri = GetUnwrappedText(context.STRING_LITERAL());
        readFunction.Arguments.Add(new FunctionCallArgumentNode("uri",
            new LiteralNode(new VariantValue(uri))));
        if (context.functionCall() != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(context.functionCall());
            readFunction.Arguments.Add(new FunctionCallArgumentNode("formatter", formatterFunctionCallNode));
        }
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return new SelectTableFunctionNode(readFunction, alias);
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTableReferenceSubquery(
        QueryCatParser.SelectTableReferenceSubqueryContext context)
    {
        var query = this.Visit<SelectQueryExpressionBodyNode>(context.selectQueryExpression());
        query.Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return query;
    }

    #endregion

    #region Group, Having

    /// <inheritdoc />
    public override IAstNode VisitSelectGroupBy(QueryCatParser.SelectGroupByContext context)
        => new SelectGroupByNode(this.Visit<ExpressionNode>(context.expression()).ToList());

    /// <inheritdoc />
    public override IAstNode VisitSelectHaving(QueryCatParser.SelectHavingContext context)
        => new SelectHavingNode(this.Visit<ExpressionNode>(context.expression()));

    #endregion

    #region Search condition

    /// <inheritdoc />
    public override IAstNode VisitSelectSearchCondition(QueryCatParser.SelectSearchConditionContext context)
        => new SelectSearchConditionNode(this.Visit<ExpressionNode>(context.expression()));

    #endregion

    #region Order

    // See: 7.17 <query expression>

    /// <inheritdoc />
    public override IAstNode VisitSelectOrderByClause(QueryCatParser.SelectOrderByClauseContext context)
        => new SelectOrderByNode(this.Visit<SelectOrderBySpecificationNode>(context.selectSortSpecification()).ToList());

    /// <inheritdoc />
    public override IAstNode VisitSelectSortSpecification(QueryCatParser.SelectSortSpecificationContext context)
        => new SelectOrderBySpecificationNode(this.Visit<ExpressionNode>(context.expression()),
            context.DESC() != null ? SelectOrderSpecification.Descending : SelectOrderSpecification.Ascending);

    #endregion

    #region Limit, offset

    // See: 7.17 <query expression>

    /// <inheritdoc />
    public override IAstNode VisitSelectOffsetClause(QueryCatParser.SelectOffsetClauseContext context)
        => new SelectOffsetNode(this.Visit<ExpressionNode>(context.offset));

    /// <inheritdoc />
    public override IAstNode VisitSelectFetchFirstClause(QueryCatParser.SelectFetchFirstClauseContext context)
        => new SelectFetchNode(this.Visit<ExpressionNode>(context.limit));

    #endregion
}
