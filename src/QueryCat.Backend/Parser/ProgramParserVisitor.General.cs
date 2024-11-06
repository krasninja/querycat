using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor : QueryCatParserBaseVisitor<IAstNode>
{
    /// <inheritdoc />
    public override IAstNode VisitProgram(QueryCatParser.ProgramContext context)
    {
        var statements = new List<StatementNode>();
        foreach (var statementContext in context.statement())
        {
            if (statementContext.IsEmpty || statementContext.children == null)
            {
                continue;
            }
            var statement = this.Visit<StatementNode>(statementContext);
            if (statement == null)
            {
                throw new InvalidOperationException(Resources.Errors.InvalidStatement);
            }
            statements.Add(statement);
        }
        // Make statements sequence.
        for (var i = 1; i < statements.Count; i++)
        {
            statements[i - 1].NextNode = statements[i];
        }
        return new ProgramNode(statements);
    }

    /// <inheritdoc />
    public override IAstNode VisitStatementExpression(QueryCatParser.StatementExpressionContext context)
        => new ExpressionStatementNode((ExpressionNode)Visit(context.expression()));

    /// <inheritdoc />
    public override IAstNode VisitStatementFunctionCall(QueryCatParser.StatementFunctionCallContext context)
        => new FunctionCallStatementNode(this.Visit<FunctionCallNode>(context.functionCall()));

    /// <inheritdoc />
    public override IAstNode VisitBlockExpression(QueryCatParser.BlockExpressionContext context)
        => new BlockExpressionNode(context.statement().Select(this.Visit<StatementNode>).ToList());

    #region Expressions and literals

    /// <inheritdoc />
    public override IAstNode VisitLiteralPlain(QueryCatParser.LiteralPlainContext context)
    {
        var text = GetUnwrappedText(context);
        return context.Start.Type switch
        {
            QueryCatParser.INTEGER_LITERAL => new LiteralNode(new VariantValue(int.Parse(text))),
            QueryCatParser.FLOAT_LITERAL => new LiteralNode(new VariantValue(double.Parse(text))),
            QueryCatParser.NUMERIC_LITERAL => new LiteralNode(new VariantValue(decimal.Parse(text))),
            QueryCatParser.STRING_LITERAL => new LiteralNode(new VariantValue(text)),
            QueryCatParser.BOOLEAN_LITERAL => new LiteralNode(VariantValue.TrueValue),
            QueryCatParser.TRUE => new LiteralNode(VariantValue.TrueValue),
            QueryCatParser.FALSE => new LiteralNode(VariantValue.FalseValue),
            QueryCatParser.NULL => new LiteralNode(VariantValue.Null),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitLiteralInterval(QueryCatParser.LiteralIntervalContext context)
        => new LiteralNode(new VariantValue(
            IntervalParser.ParseInterval(GetUnwrappedText(context.intervalLiteral().interval))));

    /// <inheritdoc />
    public override IAstNode VisitExpressionBinary(QueryCatParser.ExpressionBinaryContext context)
    {
        var operation = ConvertOperationTokenToAst(context.op.Type);
        var not = context.NOT();
        if (operation == VariantValue.Operation.Like && not != null)
        {
            operation = VariantValue.Operation.NotLike;
        }
        else if (operation == VariantValue.Operation.Similar && not != null)
        {
            operation = VariantValue.Operation.NotSimilar;
        }

        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new BinaryOperationExpressionNode(operation, left, right);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionBinaryCast(QueryCatParser.ExpressionBinaryCastContext context)
        => new CastFunctionNode(
            this.Visit<ExpressionNode>(context.right),
            this.VisitType(context.type())
        );

    /// <inheritdoc />
    public override IAstNode VisitExpressionAtTimeZone(QueryCatParser.ExpressionAtTimeZoneContext context)
    {
        var tzNode = context.atTimeZone().LOCAL() == null
            ? (ExpressionNode)Visit(context.atTimeZone().tz)
            : new LiteralNode(new VariantValue(TimeZoneInfo.Local.Id));
        return new AtTimeZoneNode(
            this.Visit<ExpressionNode>(context.left),
            tzNode);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionUnary(QueryCatParser.ExpressionUnaryContext context)
    {
        var operation = ConvertOperationTokenToAst(context.op.Type);
        if (operation == VariantValue.Operation.IsNull && context.NOT() != null)
        {
            operation = VariantValue.Operation.IsNotNull;
        }

        var right = (ExpressionNode)Visit(context.right);
        return new UnaryOperationExpressionNode(operation, right);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionInParens(QueryCatParser.ExpressionInParensContext context)
        => Visit(context.expression());

    /// <inheritdoc />
    public override IAstNode VisitExpressionBetween(QueryCatParser.ExpressionBetweenContext context)
    {
        var expression = (ExpressionNode)Visit(context.expr);
        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new BetweenExpressionNode(expression,
            new BinaryOperationExpressionNode(VariantValue.Operation.And, left, right),
            isNot: context.NOT() != null);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionBinaryInArray(QueryCatParser.ExpressionBinaryInArrayContext context)
    {
        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new InOperationExpressionNode(left, right, isNot: context.NOT() != null);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionBinaryInSubquery(QueryCatParser.ExpressionBinaryInSubqueryContext context)
    {
        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new InOperationExpressionNode(left, right, isNot: context.NOT() != null);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionStandardFunctionCall(QueryCatParser.ExpressionStandardFunctionCallContext context)
        => this.Visit<ExpressionNode>(context.standardFunction());

    /// <inheritdoc />
    public override IAstNode VisitExpressionFunctionCall(QueryCatParser.ExpressionFunctionCallContext context)
        => this.Visit<FunctionCallNode>(context.functionCall());

    /// <inheritdoc />
    public override IAstNode VisitSimpleExpressionUnary(QueryCatParser.SimpleExpressionUnaryContext context)
    {
        var operation = ConvertOperationTokenToAst(context.op.Type);
        if (operation == VariantValue.Operation.IsNull)
        {
            operation = VariantValue.Operation.IsNotNull;
        }

        var right = (ExpressionNode)Visit(context.right);
        return new UnaryOperationExpressionNode(operation, right);
    }

    /// <inheritdoc />
    public override IAstNode VisitSimpleExpressionBinary(QueryCatParser.SimpleExpressionBinaryContext context)
    {
        var operation = ConvertOperationTokenToAst(context.op.Type);
        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new BinaryOperationExpressionNode(operation, left, right);
    }

    /// <inheritdoc />
    public override IAstNode VisitSimpleExpressionFunctionCall(QueryCatParser.SimpleExpressionFunctionCallContext context)
        => this.Visit<FunctionCallNode>(context.functionCall());

    /// <inheritdoc />
    public override IAstNode VisitSimpleExpressionStandardFunctionCall(QueryCatParser.SimpleExpressionStandardFunctionCallContext context)
        => this.Visit<FunctionCallNode>(context.standardFunction());

    /// <inheritdoc />
    public override IAstNode VisitSimpleExpressionInParens(QueryCatParser.SimpleExpressionInParensContext context)
        => Visit(context.simpleExpression());

    /// <inheritdoc />
    public override IAstNode VisitCastOperand(QueryCatParser.CastOperandContext context)
        => new CastFunctionNode(
            this.Visit<ExpressionNode>(context.value),
            this.VisitType(context.type())
        );

    /// <inheritdoc />
    public override IAstNode VisitCaseExpression(QueryCatParser.CaseExpressionContext context)
        => new CaseExpressionNode(
            argumentNode: this.VisitMaybe<ExpressionNode?>(context.arg),
            when: this.Visit<CaseWhenThenNode>(context.caseWhen()),
            @default: this.Visit<ExpressionNode>(context.@default));

    /// <inheritdoc />
    public override IAstNode VisitCaseWhen(QueryCatParser.CaseWhenContext context)
        => new CaseWhenThenNode(
            conditionNode: this.Visit<ExpressionNode>(context.condition),
            resultNode: this.Visit<ExpressionNode>(context.result));

    #endregion

    /// <summary>
    /// Convert token to AST operation.
    /// </summary>
    /// <param name="token">Lexer token.</param>
    /// <returns>Operation.</returns>
    private static VariantValue.Operation ConvertOperationTokenToAst(int token) => token switch
    {
        QueryCatParser.DIV => VariantValue.Operation.Divide,
        QueryCatParser.STAR => VariantValue.Operation.Multiple,
        QueryCatParser.MOD => VariantValue.Operation.Modulo,
        QueryCatParser.PLUS => VariantValue.Operation.Add,
        QueryCatParser.MINUS => VariantValue.Operation.Subtract,
        QueryCatParser.LESS_LESS => VariantValue.Operation.LeftShift,
        QueryCatParser.GREATER_GREATER => VariantValue.Operation.RightShift,
        QueryCatParser.EQUALS => VariantValue.Operation.Equals,
        QueryCatParser.NOT_EQUALS => VariantValue.Operation.NotEquals,
        QueryCatParser.GREATER => VariantValue.Operation.Greater,
        QueryCatParser.GREATER_OR_EQUALS => VariantValue.Operation.GreaterOrEquals,
        QueryCatParser.LESS => VariantValue.Operation.Less,
        QueryCatParser.LESS_OR_EQUALS => VariantValue.Operation.LessOrEquals,
        QueryCatParser.AND => VariantValue.Operation.And,
        QueryCatParser.OR => VariantValue.Operation.Or,
        QueryCatParser.NOT => VariantValue.Operation.Not,
        QueryCatParser.CONCAT => VariantValue.Operation.Concat,
        QueryCatParser.BETWEEN => VariantValue.Operation.Between,
        QueryCatParser.IS => VariantValue.Operation.IsNull,
        QueryCatParser.LIKE => VariantValue.Operation.Like,
        QueryCatParser.SIMILAR => VariantValue.Operation.Similar,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, Resources.Errors.InvalidOperation)
    };

    /// <inheritdoc />
    public override IAstNode VisitArray(QueryCatParser.ArrayContext context)
        => new InExpressionValuesNode(this.Visit<ExpressionNode>(context.expression()));

    /// <inheritdoc />
    public override IAstNode VisitIdentifierSimpleNoQuotes(QueryCatParser.IdentifierSimpleNoQuotesContext context)
        => new IdentifierExpressionNode(GetUnwrappedText(context.NO_QUOTES_IDENTIFIER()));

    /// <inheritdoc />
    public override IAstNode VisitIdentifierSimpleQuotes(QueryCatParser.IdentifierSimpleQuotesContext context)
        => new IdentifierExpressionNode(GetUnwrappedText(context.QUOTES_IDENTIFIER()));

    /// <inheritdoc />
    public override IAstNode VisitIdentifierSimpleCurrent(QueryCatParser.IdentifierSimpleCurrentContext context)
        => new IdentifierExpressionNode(IdentifierExpressionNode.CurrentSymbol);

    /// <inheritdoc />
    public override IAstNode VisitIdentifierWithoutSource(QueryCatParser.IdentifierWithoutSourceContext context)
        => new IdentifierExpressionNode(GetUnwrappedText(context.name));

    /// <inheritdoc />
    public override IAstNode VisitIdentifierWithSelector(QueryCatParser.IdentifierWithSelectorContext context)
        => new IdentifierExpressionNode(
            name: GetUnwrappedText(context.name),
            selectorNodes: context.identifierSelector().Select(this.Visit<IdentifierSelectorNode>).ToList());

    /// <inheritdoc />
    public override IAstNode VisitIdentifierSelectorProperty(QueryCatParser.IdentifierSelectorPropertyContext context)
        => new IdentifierPropertySelectorNode(propertyName: GetUnwrappedText(context.name));

    /// <inheritdoc />
    public override IAstNode VisitIdentifierSelectorIndex(QueryCatParser.IdentifierSelectorIndexContext context)
        => new IdentifierIndexSelectorNode(
            indexExpression: context.simpleExpression().Select(this.Visit<ExpressionNode>).ToList());

    /// <inheritdoc />
    public override IAstNode VisitIdentifierSelectorFilterExpression(QueryCatParser.IdentifierSelectorFilterExpressionContext context)
        => new IdentifierFilterSelectorNode(this.Visit<BinaryOperationExpressionNode>(context.simpleExpression()));

    private static bool GetBooleanFromString(string text)
    {
        if (text.Equals(VariantValue.TrueValueString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (text.Equals(VariantValue.FalseValueString, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        throw new ArgumentOutOfRangeException(nameof(text));
    }

    #region Functions

    /// <inheritdoc />
    public override IAstNode VisitFunctionCall(QueryCatParser.FunctionCallContext context)
        => new FunctionCallNode(GetUnwrappedText(context.identifierSimple()),
            this.Visit<FunctionCallArgumentNode>(context.functionCallArg()));

    /// <inheritdoc />
    public override IAstNode VisitFunctionCallArg(QueryCatParser.FunctionCallArgContext context)
        => new FunctionCallArgumentNode(
            GetUnwrappedText(context.identifierSimple()), this.Visit<ExpressionNode>(context.expression()));

    #endregion

    #region Standard functions

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionCurrentDate(QueryCatParser.StandardFunctionCurrentDateContext context)
        => new FunctionCallNode("date", new FunctionCallArgumentNode(new FunctionCallNode("now")));

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionCurrentTimestamp(QueryCatParser.StandardFunctionCurrentTimestampContext context)
        => new FunctionCallNode("now");

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionTrim(QueryCatParser.StandardFunctionTrimContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.target);
        var characters = context.characters != null ? context.characters.Text : string.Empty;
        if (context.spec == null || context.spec.Type == QueryCatLexer.BOTH)
        {
            return new FunctionCallNode("btrim",
                new FunctionCallArgumentNode(targetNode),
                new FunctionCallArgumentNode(new LiteralNode(characters)));
        }
        if (context.spec.Type == QueryCatLexer.LEADING)
        {
            return new FunctionCallNode("ltrim",
                new FunctionCallArgumentNode(targetNode),
                new FunctionCallArgumentNode(new LiteralNode(characters)));
        }
        if (context.spec.Type == QueryCatLexer.TRAILING)
        {
            return new FunctionCallNode("rtrim",
                new FunctionCallArgumentNode(targetNode),
                new FunctionCallArgumentNode(new LiteralNode(characters)));
        }
        return base.VisitStandardFunctionTrim(context);
    }

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionPosition(QueryCatParser.StandardFunctionPositionContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.@string);
        return new FunctionCallNode("position",
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.substring))),
            new FunctionCallArgumentNode(targetNode));
    }

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionExtract(QueryCatParser.StandardFunctionExtractContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.source);
        return new FunctionCallNode("date_part",
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.extractField))),
            new FunctionCallArgumentNode(targetNode));
    }

    /// <inheritdoc />
    public override IAstNode VisitStandardOccurrencesRegex(QueryCatParser.StandardOccurrencesRegexContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.@string);
        return new FunctionCallNode("regexp_count",
            new FunctionCallArgumentNode(targetNode),
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.pattern))));
    }

    /// <inheritdoc />
    public override IAstNode VisitStandardSubstringRegex(QueryCatParser.StandardSubstringRegexContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.@string);
        return new FunctionCallNode("regexp_substr",
            new FunctionCallArgumentNode(targetNode),
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.pattern))));
    }

    /// <inheritdoc />
    public override IAstNode VisitStandardTranslateRegex(QueryCatParser.StandardTranslateRegexContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.@string);
        return new FunctionCallNode("regexp_replace",
            new FunctionCallArgumentNode(targetNode),
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.pattern))),
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.replacement))));
    }

    #endregion

    /// <inheritdoc />
    public override TypeNode VisitType(QueryCatParser.TypeContext context)
    {
        var value = GetChildType(context, 0) switch
        {
            QueryCatParser.INTEGER => DataType.Integer,
            QueryCatParser.INT => DataType.Integer,
            QueryCatParser.INT8 => DataType.Integer,
            QueryCatParser.FLOAT => DataType.Float,
            QueryCatParser.REAL => DataType.Float,
            QueryCatParser.NUMERIC => DataType.Numeric,
            QueryCatParser.DECIMAL => DataType.Numeric,
            QueryCatParser.STRING => DataType.String,
            QueryCatParser.TEXT => DataType.String,
            QueryCatParser.BLOB => DataType.Blob,
            QueryCatParser.BOOLEAN => DataType.Boolean,
            QueryCatParser.BOOL => DataType.Boolean,
            QueryCatParser.TIMESTAMP => DataType.Timestamp,
            QueryCatParser.INTERVAL => DataType.Interval,
            QueryCatParser.ANY => DataType.Dynamic,
            QueryCatParser.VOID => DataType.Void,
            _ => DataType.Object,
        };
        return new TypeNode(value);
    }

    #region Utils

    private static int GetChildType(ParserRuleContext context, int index)
        => ((ITerminalNode)context.GetChild(index)).Symbol.Type;

    private static string GetUnwrappedText(IParseTree? node)
    {
        if (node == null)
        {
            return string.Empty;
        }
        var text = node.GetText();
        if (text.StartsWith("E\'", StringComparison.InvariantCultureIgnoreCase))
        {
            text = StringUtils.Unescape(text.Substring(2, text.Length - 3));
        }
        return StringUtils.GetUnwrappedText(text);
    }

    private static string GetUnwrappedText(IToken? token)
    {
        if (token == null)
        {
            return string.Empty;
        }
        return StringUtils.GetUnwrappedText(token.Text);
    }

    #endregion
}
