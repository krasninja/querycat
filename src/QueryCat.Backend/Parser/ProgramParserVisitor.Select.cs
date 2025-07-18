using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    private const string StdinFunctionName = "stdin";
    private const string StdoutFunctionName = "stdout";

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
    {
        var name = context.identifierSimple() != null && !context.identifierSimple().IsEmpty
            ? GetUnwrappedText(context.identifierSimple())
            : GetUnwrappedText(context.STRING_LITERAL());
        return new SelectAliasNode(name);
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectQueryExpressionFull(QueryCatParser.SelectQueryExpressionFullContext context)
    {
        var query = this.Visit<SelectQueryNode>(context.selectQueryExpressionBody());
        query.WithNode = this.VisitMaybe<SelectWithListNode>(context.selectWithClause());
        query.OrderByNode = this.VisitMaybe<SelectOrderByNode>(context.selectOrderByClause());
        query.OffsetNode = this.VisitMaybe<SelectOffsetNode>(context.selectOffsetClause());
        query.FetchNode = query.FetchNode
            ?? this.VisitMaybe<SelectFetchNode>(context.selectFetchFirstClause())
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
    {
        var tableExpressionNode = this.VisitMaybe<SelectTableNode>(context.selectFromClause());
        if (tableExpressionNode == null && Console.IsInputRedirected && !Console.IsErrorRedirected && !Console.IsOutputRedirected)
        {
            tableExpressionNode = new SelectTableNode(new SelectTableReferenceListNode(
                new List<ExpressionNode>
                {
                    new SelectTableFunctionNode(new FunctionCallNode(StdinFunctionName)),
                }));
        }
        return new SelectQuerySpecificationNode(this.Visit<SelectColumnsListNode>(context.selectList()))
        {
            WithNode = this.VisitMaybe<SelectWithListNode>(context.selectWithClause()),
            DistinctNode = this.VisitMaybe<SelectDistinctNode>(context.selectDistinctClause()),
            TableExpressionNode = tableExpressionNode,
            TargetNode = this.VisitMaybe<FunctionCallNode>(context.selectTarget()),
            FetchNode = this.VisitMaybe<SelectFetchNode>(context.selectTopClause()),
            ExceptIdentifiersNode = this.VisitMaybe<SelectColumnsExceptNode>(context.selectExcept()),
            WindowNode = this.VisitMaybe<SelectWindowNode>(context.selectWindow()),
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectList(QueryCatParser.SelectListContext context)
        => new SelectColumnsListNode(this.Visit<SelectColumnsSublistNode>(context.selectSublist()));

    /// <inheritdoc />
    public override IAstNode VisitSelectExcept(QueryCatParser.SelectExceptContext context)
        => new SelectColumnsExceptNode(this.Visit<IdentifierExpressionNode>(context.identifier()));

    #endregion

    #region With

    /// <inheritdoc />
    public override IAstNode VisitSelectWithClause(QueryCatParser.SelectWithClauseContext context)
        => new SelectWithListNode(
            this.Visit<SelectWithNode>(context.selectWithElement()),
            isRecursive: context.RECURSIVE() != null);

    /// <inheritdoc />
    public override IAstNode VisitSelectWithElement(QueryCatParser.SelectWithElementContext context)
    {
        var selectWithNode = new SelectWithNode(
            name: GetUnwrappedText(context.name),
            queryNode: this.Visit<SelectQueryNode>(context.query));
        selectWithNode.ColumnNodes.AddRange(
            this.Visit(context.selectWithColumnList(), new SelectColumnsListNode()).ColumnsNodes);
        return selectWithNode;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectWithColumnList(QueryCatParser.SelectWithColumnListContext context)
    {
        var columnsNodes = this.Visit<IdentifierExpressionNode>(context.identifier())
            .Select(n => new SelectColumnsSublistExpressionNode(n));
        return new SelectColumnsListNode(columnsNodes);
    }

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
        return new SelectDistinctNode(this.Visit<ExpressionNode>(context.simpleExpression()));
    }

    #endregion

    #region Columns

    // 7.16 <query specification>

    /// <inheritdoc />
    public override IAstNode VisitSelectSublistAll(QueryCatParser.SelectSublistAllContext context)
        => new SelectColumnsSublistAll
        {
            PrefixIdentifier = this.VisitMaybe<IdentifierExpressionNode>(context.identifierSimple())
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectSublistWindow(QueryCatParser.SelectSublistWindowContext context)
        => new SelectColumnsSublistWindowNode(
            this.Visit<FunctionCallNode>(context.functionCall()),
            this.Visit<SelectWindowSpecificationNode>(context.selectWindowSpecification()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectSublistExpression(QueryCatParser.SelectSublistExpressionContext context)
        => new SelectColumnsSublistExpressionNode((ExpressionNode)Visit(context.expression()))
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName
        };

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
            leftNode: this.Visit<ExpressionNode>(context.left),
            operation: operation,
            quantifierOperator: ConvertStringToOperation(context.condition.Type),
            subQueryNode: this.Visit<SelectQueryNode>(context.selectQueryExpression()));
    }

    #endregion

    #region Into

    /// <inheritdoc />
    public override IAstNode VisitSelectTarget(QueryCatParser.SelectTargetContext context)
    {
        FunctionCallNode? functionCallNode;
        if (context.uri != null)
        {
            functionCallNode = new FunctionCallNode("write");
            var uri = GetUnwrappedText(context.uri);
            functionCallNode.Arguments.Add(new FunctionCallArgumentNode("uri",
                new LiteralNode(new VariantValue(uri))));
        }
        else if (context.into != null)
        {
            functionCallNode = this.Visit<FunctionCallNode>(context.into);
        }
        else if (context.expr != null)
        {
            functionCallNode = new FunctionCallNode("write");
            functionCallNode.Arguments.Add(new FunctionCallArgumentNode(
                this.Visit<ExpressionNode>(context.expr)));
        }
        else
        {
            functionCallNode = new FunctionCallNode(StdoutFunctionName);
        }
        if (context.format != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(context.format);
            functionCallNode.Arguments.Add(new FunctionCallArgumentNode("fmt", formatterFunctionCallNode));
        }
        return functionCallNode;
    }

    #endregion

    #region From

    // 7.5 <from clause>
    // 7.6 <table reference>

    /// <inheritdoc />
    public override IAstNode VisitSelectFromClause(QueryCatParser.SelectFromClauseContext context)
        => new SelectTableNode(this.Visit<SelectTableReferenceListNode>(context.selectTableReferenceList()))
        {
            SearchConditionNode = this.VisitMaybe<SelectSearchConditionNode>(context.selectSearchCondition()),
            GroupByNode = this.VisitMaybe<SelectGroupByNode>(context.selectGroupBy()),
            HavingNode = this.VisitMaybe<SelectHavingNode>(context.selectHaving()),
        };

    /// <inheritdoc />
    public override IAstNode VisitSelectTableReferenceList(QueryCatParser.SelectTableReferenceListContext context)
        => new SelectTableReferenceListNode(this.Visit<ExpressionNode>(context.selectTableReference()));

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryNoFormat(QueryCatParser.SelectTablePrimaryNoFormatContext context)
    {
        var functionCallNode = this.Visit<FunctionCallNode>(context.func);
        if (context.format != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(context.format);
            functionCallNode.Arguments.Add(new FunctionCallArgumentNode("fmt", formatterFunctionCallNode));
        }
        return new SelectTableFunctionNode(functionCallNode)
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName,
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryStdin(QueryCatParser.SelectTablePrimaryStdinContext context)
    {
        var functionCallNode = new FunctionCallNode(StdinFunctionName);
        if (context.format != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(context.format);
            functionCallNode.Arguments.Add(new FunctionCallArgumentNode("fmt", formatterFunctionCallNode));
        }
        return new SelectTableFunctionNode(functionCallNode)
        {
            Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName,
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryWithFormat(QueryCatParser.SelectTablePrimaryWithFormatContext context)
    {
        var uri = GetUnwrappedText(context.uri);
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        var readFunctionNode = CreateReadFunctionCallNode(
            uriNode: new LiteralNode(new VariantValue(uri)),
            formatContext: context.format);
        return new SelectTableFunctionNode(readFunctionNode, alias);
    }

    private FunctionCallNode CreateReadFunctionCallNode(ExpressionNode uriNode, QueryCatParser.FunctionCallContext? formatContext = null)
    {
        var readFunctionNode = new FunctionCallNode("read");
        readFunctionNode.Arguments.Add(new FunctionCallArgumentNode("uri", uriNode));
        if (formatContext != null)
        {
            var formatterFunctionCallNode = this.Visit<FunctionCallNode>(formatContext);
            readFunctionNode.Arguments.Add(new FunctionCallArgumentNode("fmt", formatterFunctionCallNode));
        }
        return readFunctionNode;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryIdentifier(QueryCatParser.SelectTablePrimaryIdentifierContext context)
    {
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return new SelectIdentifierExpressionNode(this.Visit<IdentifierExpressionNode>(context.name), alias)
        {
            Format = context.format != null ? this.Visit<FunctionCallNode>(context.format) : null,
        };
    }

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
        if (expressionNode is ISelectJoinedNode functionNode)
        {
            functionNode.JoinedNodes.AddRange(this.Visit<SelectTableJoinedNode>(context.selectTableJoined()));
        }
        return expressionNode;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTableJoinedOn(QueryCatParser.SelectTableJoinedOnContext context)
    {
        var joinTypeNode = context.selectJoinType() != null
            ? this.Visit<SelectTableJoinedTypeNode>(context.selectJoinType())
            : new SelectTableJoinedTypeNode(SelectTableJoinedType.Inner);
        return new SelectTableJoinedOnNode(
            this.Visit<ExpressionNode>(context.right),
            joinTypeNode,
            this.Visit<ExpressionNode>(context.condition));
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTableJoinedUsing(QueryCatParser.SelectTableJoinedUsingContext context)
    {
        if (context.identifier().Length < 1)
        {
            throw new SemanticException(Resources.Errors.NoUsingJoinColumns);
        }

        return new SelectTableJoinedUsingNode(
            this.Visit<ExpressionNode>(context.right),
            this.Visit<SelectTableJoinedTypeNode>(context.selectJoinType()),
            context.identifier().Select(GetUnwrappedText));
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

    /// <inheritdoc />
    public override IAstNode VisitSelectTableValuesRow(QueryCatParser.SelectTableValuesRowContext context)
        => new SelectTableValuesRowNode(this.Visit<ExpressionNode>(context.simpleExpression()));

    /// <inheritdoc />
    public override IAstNode VisitSelectTableValues(QueryCatParser.SelectTableValuesContext context)
        => new SelectTableValuesNode(this.Visit<SelectTableValuesRowNode>(context.selectTableValuesRow()));

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryTableValues(QueryCatParser.SelectTablePrimaryTableValuesContext context)
    {
        var query = this.Visit<SelectTableValuesNode>(context.selectTableValues());
        query.Alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        return query;
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectTablePrimaryExpression(QueryCatParser.SelectTablePrimaryExpressionContext context)
    {
        var alias = this.Visit(context.selectAlias(), SelectAliasNode.Empty).AliasName;
        var readFunctionNode = CreateReadFunctionCallNode(
            uriNode: this.Visit<ExpressionNode>(context.simpleExpression()),
            formatContext: context.format);
        return new SelectTableFunctionNode(readFunctionNode, alias);
    }

    #endregion

    #region Group, Having

    /// <inheritdoc />
    public override IAstNode VisitSelectGroupBy(QueryCatParser.SelectGroupByContext context)
        => new SelectGroupByNode(this.Visit<ExpressionNode>(context.expression()));

    /// <inheritdoc />
    public override IAstNode VisitSelectHaving(QueryCatParser.SelectHavingContext context)
        => new SelectHavingNode(this.Visit<ExpressionNode>(context.expression()));

    #endregion

    #region Search condition

    /// <inheritdoc />
    public override IAstNode VisitSelectSearchCondition(QueryCatParser.SelectSearchConditionContext context)
        => new SelectSearchConditionNode(this.Visit<ExpressionNode>(context.expression()));

    #endregion

    #region Window

    /// <inheritdoc />
    public override IAstNode VisitSelectWindowSpecification(QueryCatParser.SelectWindowSpecificationContext context)
    {
        if (context.existingWindowName != null)
        {
            return new SelectWindowSpecificationNode(GetUnwrappedText(context.existingWindowName));
        }
        return new SelectWindowSpecificationNode(
            this.VisitMaybe<SelectWindowPartitionClauseNode>(context.selectWindowPartitionClause()),
            this.VisitMaybe<SelectWindowOrderClauseNode>(context.selectWindowOrderClause()));
    }

    /// <inheritdoc />
    public override IAstNode VisitSelectWindowPartitionClause(QueryCatParser.SelectWindowPartitionClauseContext context)
        => new SelectWindowPartitionClauseNode(this.Visit<ExpressionNode>(context.expression()));

    /// <inheritdoc />
    public override IAstNode VisitSelectWindowOrderClause(QueryCatParser.SelectWindowOrderClauseContext context)
        => new SelectWindowOrderClauseNode(this.Visit<SelectOrderBySpecificationNode>(context.selectSortSpecification()));

    /// <inheritdoc />
    public override IAstNode VisitSelectWindow(QueryCatParser.SelectWindowContext context)
        => new SelectWindowNode(this.Visit<SelectWindowDefinitionListNode>(context.selectWindowDefinitionList()));

    /// <inheritdoc />
    public override IAstNode VisitSelectWindowDefinitionList(QueryCatParser.SelectWindowDefinitionListContext context)
        => new SelectWindowDefinitionListNode(
            GetUnwrappedText(context.name),
            this.Visit<SelectWindowSpecificationNode>(context.selectWindowSpecification()));

    #endregion

    #region Order

    // See: 7.17 <query expression>

    /// <inheritdoc />
    public override IAstNode VisitSelectOrderByClause(QueryCatParser.SelectOrderByClauseContext context)
        => new SelectOrderByNode(this.Visit<SelectOrderBySpecificationNode>(context.selectSortSpecification()));

    /// <inheritdoc />
    public override IAstNode VisitSelectSortSpecification(QueryCatParser.SelectSortSpecificationContext context)
    {
        var nullOrder = SelectNullOrder.NullsLast;
        if (context.LAST() != null)
        {
            nullOrder = SelectNullOrder.NullsLast;
        }
        else if (context.FIRST() != null)
        {
            nullOrder = SelectNullOrder.NullsFirst;
        }
        return new SelectOrderBySpecificationNode(
            expressionNode: this.Visit<ExpressionNode>(context.expression()),
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
                new VariantValue(GetUnwrappedText(context.limit)))
            );

    #endregion
}
