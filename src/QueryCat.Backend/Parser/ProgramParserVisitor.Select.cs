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
        => new SelectStatementNode((SelectQueryNode)Visit(context.selectStatement()));

    #endregion

    #region Select

    /// <inheritdoc />
    public override IAstNode VisitSelectStatement(QueryCatParser.SelectStatementContext context)
        => this.Visit<SelectQueryNode>(context.selectQueryExpression());

    /// <inheritdoc />
    public override IAstNode VisitSelectAlias(QueryCatParser.SelectAliasContext context)
        => new SelectAliasNode(GetUnwrappedText(context.name.Text));

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpressionSimple(QueryCatParser.SelectQueryExpressionSimpleContext context)
        => new SelectQuerySpecificationNode(this.Visit<SelectColumnsListNode>(context.selectList()))
        {
            WithNode = this.VisitMaybe<SelectWithListNode>(context.selectWithClause()),
            DistinctNode = this.VisitMaybe<SelectDistinctNode>(context.selectDistinctClause()),
            TableExpressionNode = this.VisitMaybe<SelectTableExpressionNode>(context.selectFromClause()),
            TargetNode = this.VisitMaybe<FunctionCallNode>(context.selectTarget()),
            OrderByNode = this.VisitMaybe<SelectOrderByNode>(context.selectOrderByClause()),
            OffsetNode = this.VisitMaybe<SelectOffsetNode>(context.selectOffsetClause()),
            FetchNode = this.VisitMaybe<SelectFetchNode>(context.selectTopClause())
                ?? this.VisitMaybe<SelectFetchNode>(context.selectFetchFirstClause())
                ?? this.VisitMaybe<SelectFetchNode>(context.selectLimitClause())
                ?? this.VisitMaybe<SelectFetchNode>(context.selectTopClause())
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpressionFull(QueryCatParser.SelectQueryExpressionFullContext context)
    {
        var query = this.Visit<SelectQueryNode>(context.selectQueryExpressionBody());
        query.OrderByNode = this.VisitMaybe<SelectOrderByNode>(context.selectOrderByClause());
        query.OffsetNode = this.VisitMaybe<SelectOffsetNode>(context.selectOffsetClause());
        query.FetchNode = this.VisitMaybe<SelectFetchNode>(context.selectFetchFirstClause())
            ?? this.VisitMaybe<SelectFetchNode>(context.selectLimitClause());
        return query;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpressionBodyPrimary(QueryCatParser.SelectQueryExpressionBodyPrimaryContext context)
        => this.Visit<SelectQueryNode>(context.left);

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpressionBodyIntersect(QueryCatParser.SelectQueryExpressionBodyIntersectContext context)
        => new SelectQueryCombineNode(
                this.Visit<SelectQueryNode>(context.left),
                this.Visit<SelectQueryNode>(context.right),
                SelectQueryCombineType.Intersect,
                isDistinct: context.ALL() == null);

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpressionBodyUnionExcept(QueryCatParser.SelectQueryExpressionBodyUnionExceptContext context)
        => new SelectQueryCombineNode(
                this.Visit<SelectQueryNode>(context.left),
                this.Visit<SelectQueryNode>(context.right),
                context.UNION() != null ? SelectQueryCombineType.Union : SelectQueryCombineType.Except,
                isDistinct: context.ALL() == null);

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryPrimaryNoParens(QueryCatParser.SelectQueryPrimaryNoParensContext context)
        => this.Visit<SelectQuerySpecificationNode>(context.selectQuerySpecification());

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryPrimaryParens(QueryCatParser.SelectQueryPrimaryParensContext context)
        => this.Visit<SelectQueryNode>(context.selectQueryExpression());

    /// <inheritdoc />
    public override IAstNode VisitSelectQuerySpecificationFull(QueryCatParser.SelectQuerySpecificationFullContext context)
        => new SelectQuerySpecificationNode(this.Visit<SelectColumnsListNode>(context.selectList()))
        {
            WithNode = this.VisitMaybe<SelectWithListNode>(context.selectWithClause()),
            DistinctNode = this.VisitMaybe<SelectDistinctNode>(context.selectDistinctClause()),
            TableExpressionNode = this.VisitMaybe<SelectTableExpressionNode>(context.selectFromClause()),
            TargetNode = this.VisitMaybe<FunctionCallNode>(context.selectTarget()),
            FetchNode = this.VisitMaybe<SelectFetchNode>(context.selectTopClause()),
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectQuerySpecificationSingle(QueryCatParser.SelectQuerySpecificationSingleContext context)
    {
        var selectColumnsSublistNodes = this.Visit<SelectColumnsSublistNode>(context.selectSublist()).ToList();
        SelectTableExpressionNode? selectTableExpressionNode = null;
        if (Console.IsInputRedirected && !Console.IsErrorRedirected && !Console.IsOutputRedirected)
        {
            selectTableExpressionNode = new SelectTableExpressionNode(new SelectTableReferenceListNode(
                new List<ExpressionNode>
                {
                    new SelectTableFunctionNode(new FunctionCallNode("stdin")),
                }));
        }
        return new SelectQuerySpecificationNode(new SelectColumnsListNode(selectColumnsSublistNodes))
        {
            TargetNode = this.VisitMaybe<FunctionCallNode>(context.selectTarget()),
            TableExpressionNode = selectTableExpressionNode
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectList(QueryCatParser.SelectListContext context)
        => new SelectColumnsListNode(this.Visit<SelectColumnsSublistNode>(context.selectSublist()).ToList());

    #endregion

    #region With

    /// <inheritdoc />
    public override IAstNode VisitSelectWithClause(QueryCatParser.SelectWithClauseContext context)
        => new SelectWithListNode(
            this.Visit<SelectWithNode>(context.selectWithElement()).ToList());

    /// <inheritdoc />
    public override IAstNode VisitSelectWithElement(QueryCatParser.SelectWithElementContext context)
        => new SelectWithNode(
            name: GetUnwrappedText(context.name.Text),
            queryNode: this.Visit<SelectQueryNode>(context.query));

    #endregion

    #region Distinct

    /// <inheritdoc />
    public override IAstNode VisitSelectDistinctClause(QueryCatParser.SelectDistinctClauseContext context)
    {
        if (context.ALL() != null)
        {
            return SelectDistinctNode.Empty;
        }
        if (context.DISTINCT() != null)
        {
            return SelectDistinctNode.All;
        }
        var distinctClause = context.selectDistinctOnClause();
        if (distinctClause != null)
        {
            return this.Visit(distinctClause);
        }
        return SelectDistinctNode.Empty;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectDistinctOnClause(QueryCatParser.SelectDistinctOnClauseContext context)
    {
        if (context.DISTINCT() == null)
        {
            return SelectDistinctNode.Empty;
        }
        return new SelectDistinctNode(this.Visit<ExpressionNode>(context.simpleExpression()).ToList());
    }

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
        => this.Visit<SelectQueryNode>(context.selectQueryExpression());

    /// <inheritdoc />
    public override IAstNode VisitExpressionExists(QueryCatParser.ExpressionExistsContext context)
        => new SelectExistsExpressionNode(this.Visit<SelectQueryNode>(context.selectQueryExpression()));

    /// <inheritdoc />
    public override IAstNode VisitExpressionSubquery(QueryCatParser.ExpressionSubqueryContext context)
    {
        SelectSubqueryConditionExpressionNode.QuantifierOperator ConvertStringToOperation(int type)
            => type switch
            {
                QueryCatParser.SOME => SelectSubqueryConditionExpressionNode.QuantifierOperator.Any,
                QueryCatParser.ANY => SelectSubqueryConditionExpressionNode.QuantifierOperator.Any,
                QueryCatParser.ALL => SelectSubqueryConditionExpressionNode.QuantifierOperator.All,
                _ => throw new ArgumentException(nameof(type)),
            };

        var operation = ConvertOperationTokenToAst(context.op.Type);
        return new SelectSubqueryConditionExpressionNode(
            left: this.Visit<ExpressionNode>(context.left),
            operation: operation,
            quantifierOperator: ConvertStringToOperation(context.condition.Type),
            subQueryNode: this.Visit<SelectQueryNode>(context.selectQueryExpression()));
    }

    #endregion

    #region Into

    /// <inheritdoc />
    public override IAstNode VisitSelectTarget(QueryCatParser.SelectTargetContext context)
    {
        if (context.uri != null)
        {
            var writeFunction = new FunctionCallNode("write");
            var uri = GetUnwrappedText(context.uri.Text);
            writeFunction.Arguments.Add(new FunctionCallArgumentNode("uri",
                new LiteralNode(new VariantValue(uri))));
            return writeFunction;
        }
        return this.Visit<FunctionCallNode>(context.functionCall());
    }

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
    public override IAstNode VisitSelectTablePrimaryNoFormat(QueryCatParser.SelectTablePrimaryNoFormatContext context)
        => new SelectTableFunctionNode(this.Visit<FunctionCallNode>(context.functionCall()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName,
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryStdin(QueryCatParser.SelectTablePrimaryStdinContext context)
        => new SelectTableFunctionNode(new FunctionCallNode("stdin"));

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryWithFormat(QueryCatParser.SelectTablePrimaryWithFormatContext context)
    {
        var readFunction = new FunctionCallNode("read");
        var uri = GetUnwrappedText(context.uri.Text);
        readFunction.Arguments.Add(new FunctionCallArgumentNode("uri",
            new LiteralNode(new VariantValue(uri))));
        if (context.functionCall() != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(context.functionCall());
            readFunction.Arguments.Add(new FunctionCallArgumentNode("fmt", formatterFunctionCallNode));
        }
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return new SelectTableFunctionNode(readFunction, alias);
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryIdentifier(QueryCatParser.SelectTablePrimaryIdentifierContext context)
        => new SelectCteIdentifierExpressionNode(GetUnwrappedText(context.name.Text));

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimarySubquery(
        QueryCatParser.SelectTablePrimarySubqueryContext context)
    {
        var query = this.Visit<SelectQueryNode>(context.selectQueryExpression());
        query.Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return query;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTableReference(QueryCatParser.SelectTableReferenceContext context)
    {
        var expressionNode = this.Visit<ExpressionNode>(context.selectTablePrimary());
        if (expressionNode is SelectTableFunctionNode functionNode)
        {
            functionNode.JoinedNodes.AddRange(this.Visit<SelectTableJoinedNode>(context.selectTableJoined()));
        }
        return expressionNode;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTableJoined(QueryCatParser.SelectTableJoinedContext context)
    {
        return new SelectTableJoinedNode(
            this.Visit<ExpressionNode>(context.right),
            this.Visit<SelectTableJoinedTypeNode>(context.selectJoinType()),
            this.Visit<ExpressionNode>(context.condition));
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectJoinType(QueryCatParser.SelectJoinTypeContext context)
    {
        var type = SelectTableJoinedType.Full;
        if (context.INNER() != null)
        {
            type = SelectTableJoinedType.Inner;
        }
        else if (context.LEFT() != null)
        {
            type = SelectTableJoinedType.Left;
        }
        else if (context.RIGHT() != null)
        {
            type = SelectTableJoinedType.Right;
        }
        return new SelectTableJoinedTypeNode(type);
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
    {
        var nullOrder = SelectNullOrdering.NullsLast;
        if (context.LAST() != null)
        {
            nullOrder = SelectNullOrdering.NullsLast;
        }
        else if (context.FIRST() != null)
        {
            nullOrder = SelectNullOrdering.NullsFirst;
        }
        return new SelectOrderBySpecificationNode(
            expression: this.Visit<ExpressionNode>(context.expression()),
            order: context.DESC() != null ? SelectOrderSpecification.Descending : SelectOrderSpecification.Ascending,
            nullOrder);
    }

    #endregion

    #region Limit, offset

    // See: 7.17 <query expression>

    /// <inheritdoc />
    public override IAstNode VisitSelectOffsetClause(QueryCatParser.SelectOffsetClauseContext context)
        => new SelectOffsetNode(this.Visit<ExpressionNode>(context.offset));

    /// <inheritdoc />
    public override IAstNode VisitSelectFetchFirstClause(QueryCatParser.SelectFetchFirstClauseContext context)
        => new SelectFetchNode(this.Visit<ExpressionNode>(context.limit));

    /// <inheritdoc />
    public override IAstNode VisitSelectLimitClause(QueryCatParser.SelectLimitClauseContext context)
        => new SelectFetchNode(this.Visit<ExpressionNode>(context.limit));

    /// <inheritdoc />
    public override IAstNode VisitSelectTopClause(QueryCatParser.SelectTopClauseContext context)
        => new SelectFetchNode(
            new LiteralNode(
                new VariantValue(GetUnwrappedText(context.limit.Text)))
            );

    #endregion
}
