using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor : QueryCatParserBaseVisitor<IAstNode>
{
    /// <inheritdoc />
    public override IAstNode VisitProgram(QueryCatParser.ProgramContext context)
    {
        var statements = new List<StatementNode>();
        StatementNode? previousStatement = null;
        foreach (var statementContext in context.statement())
        {
            if (statementContext.IsEmpty || statementContext.children == null)
            {
                continue;
            }
            var statement = this.Visit<StatementNode>(statementContext);
            if (statement == null)
            {
                throw new InvalidOperationException("Invalid statement.");
            }
            statement.Next = previousStatement;
            statements.Add(statement);
            previousStatement = statement;
        }
        return new ProgramNode(statements);
    }

    /// <inheritdoc />
    public override IAstNode VisitStatementExpression(QueryCatParser.StatementExpressionContext context)
        => new ExpressionStatementNode((ExpressionNode)Visit(context.expression()));

    /// <inheritdoc />
    public override IAstNode VisitStatementFunctionCall(QueryCatParser.StatementFunctionCallContext context)
        => new FunctionCallStatementNode(this.Visit<FunctionCallNode>(context.functionCall()));

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
            QueryCatParser.BOOLEAN_LITERAL => new LiteralNode(new VariantValue(true)),
            QueryCatParser.TRUE => new LiteralNode(new VariantValue(true)),
            QueryCatParser.FALSE => new LiteralNode(new VariantValue(false)),
            QueryCatParser.NULL => new LiteralNode(VariantValue.Null),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <inheritdoc />
    public override IAstNode VisitLiteralInterval(QueryCatParser.LiteralIntervalContext context)
        => new LiteralNode(new VariantValue(
            DataTypeUtils.ParseInterval(GetUnwrappedText(context.intervalLiteral().interval.Text))));

    /// <inheritdoc />
    public override IAstNode VisitExpressionBinary(QueryCatParser.ExpressionBinaryContext context)
    {
        var operation = ConvertOperationTokenToAst(context.op.Type);
        if (operation == VariantValue.Operation.Like && context.NOT() != null)
        {
            operation = VariantValue.Operation.NotLike;
        }

        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new BinaryOperationExpressionNode(operation, left, right);
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
    public override IAstNode VisitExpressionBinaryIn(QueryCatParser.ExpressionBinaryInContext context)
    {
        var left = (ExpressionNode)Visit(context.left);
        var right = (ExpressionNode)Visit(context.right);
        return new InOperationExpressionNode(left, right, isNot: context.NOT() != null);
    }

    /// <inheritdoc />
    public override IAstNode VisitExpressionStandardFunctionCall(QueryCatParser.ExpressionStandardFunctionCallContext context)
        => this.Visit<FunctionCallNode>(context.standardFunction());

    /// <inheritdoc />
    public override IAstNode VisitExpressionFunctionCall(QueryCatParser.ExpressionFunctionCallContext context)
        => this.Visit<FunctionCallNode>(context.functionCall());

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
    public override IAstNode VisitCastOperand(QueryCatParser.CastOperandContext context)
        => new CastNode(
            this.Visit<ExpressionNode>(context.value),
            this.VisitType(context.type())
        );

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
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, Resources.Errors.InvalidOperation)
    };

    /// <inheritdoc />
    public override IAstNode VisitArray(QueryCatParser.ArrayContext context)
        => new InExpressionValuesNode(this.Visit<ExpressionNode>(context.expression()).ToList());

    /// <inheritdoc />
    public override IAstNode VisitIdentifierChainFull(QueryCatParser.IdentifierChainFullContext context)
        => new IdentifierExpressionNode(context.name.Text, GetUnwrappedText(context.source.Text));

    /// <inheritdoc />
    public override IAstNode VisitIdentifierChainSimple(QueryCatParser.IdentifierChainSimpleContext context)
        => new IdentifierExpressionNode(GetUnwrappedText(context.name.Text));

    private static bool GetBooleanFromString(string text)
    {
        if (text.Equals(VariantValue.TrueValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (text.Equals(VariantValue.FalseValue, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        throw new ArgumentOutOfRangeException(nameof(text));
    }

    #region Functions

    /// <inheritdoc />
    public override IAstNode VisitFunctionCall(QueryCatParser.FunctionCallContext context)
        => new FunctionCallNode(GetUnwrappedText(context.IDENTIFIER()),
            this.Visit<FunctionCallArgumentNode>(context.functionCallArg()).ToList());

    /// <inheritdoc />
    public override IAstNode VisitFunctionCallArg(QueryCatParser.FunctionCallArgContext context)
        => new FunctionCallArgumentNode(
            GetUnwrappedText(context.IDENTIFIER()), this.Visit<ExpressionNode>(context.expression()));

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
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.substring.Text))),
            new FunctionCallArgumentNode(targetNode));
    }

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionExtract(QueryCatParser.StandardFunctionExtractContext context)
    {
        var targetNode = this.Visit<ExpressionNode>(context.source);
        return new FunctionCallNode("date_part",
            new FunctionCallArgumentNode(new LiteralNode(GetUnwrappedText(context.extractField.GetText()))),
            new FunctionCallArgumentNode(targetNode));
    }

    #endregion

    /// <inheritdoc />
    public override TypeNode VisitType(QueryCatParser.TypeContext context)
    {
        var value = GetChildType(context, 0) switch
        {
            QueryCatParser.INTEGER => DataType.Integer,
            QueryCatParser.FLOAT => DataType.Float,
            QueryCatParser.NUMERIC => DataType.Numeric,
            QueryCatParser.STRING => DataType.String,
            QueryCatParser.BOOLEAN => DataType.Boolean,
            QueryCatParser.TIMESTAMP => DataType.Timestamp,
            QueryCatParser.INTERVAL => DataType.Interval,
            QueryCatParser.ANY => DataType.Void,
            _ => DataType.Object,
        };
        return new TypeNode(value);
    }

    #region Utils

    private static int GetChildType(ParserRuleContext context, int index)
        => ((ITerminalNode)context.GetChild(index)).Symbol.Type;

    private static string GetUnwrappedText(string text)
    {
        if (text.StartsWith("\'", StringComparison.Ordinal) && text.EndsWith("\'", StringComparison.Ordinal))
        {
            return text.Substring(1, text.Length - 2);
        }
        if (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }

    private static string GetUnwrappedText(IParseTree? node)
    {
        if (node == null)
        {
            return string.Empty;
        }
        return GetUnwrappedText(node.GetText());
    }

    #endregion
}
