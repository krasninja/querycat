using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Parser;

/// <summary>
/// Build AST from query string.
/// </summary>
internal sealed class AstBuilder : IAstBuilder
{
    private readonly QueryCatLexer _lexer = new(new AntlrInputStream(string.Empty), TextWriter.Null, TextWriter.Null);
    private readonly QueryCatParser _parser;
    private readonly ProgramAntlrErrorListener _errorListener = new();
    private readonly ProgramParserVisitor _programParserVisitor = new();
    private readonly CommonTokenStream _tokenStream;
    private readonly BailErrorStrategy _bailErrorStrategy = new();
    private readonly DefaultErrorStrategy _defaultErrorStrategy = new();

    /// <summary>
    /// Collect profile information. Use DumpProfileInfo() method.
    /// </summary>
    public bool ProfileMode { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public AstBuilder()
    {
        _tokenStream = new CommonTokenStream(_lexer);
        _parser = new QueryCatParser(_tokenStream, TextWriter.Null, TextWriter.Null);
        _parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
        _parser.TokenStream = _tokenStream;
        _parser.RemoveErrorListeners();
        _parser.AddErrorListener(_errorListener);
        _lexer.RemoveErrorListeners();
        _lexer.AddErrorListener(_errorListener);
    }

    /// <inheritdoc />
    public ProgramNode BuildProgramFromString(string program)
        => Build<ProgramNode>(program, p => p.program());

    /// <inheritdoc />
    public FunctionSignatureNode BuildFunctionSignatureFromString(string function)
        => Build<FunctionSignatureNode>(function, p => p.functionSignature());

    /// <inheritdoc />
    public IReadOnlyList<IAstBuilder.Token> GetTokens(string text)
    {
        var inputStream = new AntlrInputStream(text);
        var lexer = new QueryCatLexer(inputStream, TextWriter.Null, TextWriter.Null);
        lexer.RemoveErrorListeners();
        var commonTokenStream = new CommonTokenStream(lexer);
        commonTokenStream.Fill();

        return TransformTokens(commonTokenStream.GetTokens());
    }

    private TNode Build<TNode>(
        string input,
        Func<QueryCatParser, ParserRuleContext> signatureFunc) where TNode : IAstNode
    {
        var resultNode = BuildInternal<TNode>(input, signatureFunc);
#if DEBUG
        // Return cloned node instead for debug only purposes.
        return (TNode)resultNode.Clone();
#else
        return resultNode;
#endif
    }

    private TNode BuildInternal<TNode>(string input, Func<QueryCatParser, ParserRuleContext> signatureFunc)
        where TNode : IAstNode
    {
        _errorListener.Clear();
        _lexer.SetInputStream(new AntlrInputStream(input));
        _tokenStream.SetTokenSource(_lexer);
        _parser.ErrorHandler = _bailErrorStrategy;
        _parser.Reset();
        _parser.Profile = ProfileMode;
        _parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
        ParserRuleContext context;
        try
        {
            context = signatureFunc.Invoke(_parser);
        }
        catch (ParseCanceledException)
        {
            // Retry with LL prediction mode that allows to parse complex queries.
            _errorListener.Clear();
            _parser.ErrorHandler = _defaultErrorStrategy;
            _parser.Reset();
            _parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.LL;
            context = signatureFunc.Invoke(_parser);
        }

        if (_errorListener.HasError)
        {
            throw new SyntaxException(_errorListener.Message, input, _errorListener.Line, _errorListener.CharPosition);
        }
        if (ProfileMode)
        {
            DumpProfileInfo();
        }

        return (TNode)_programParserVisitor.Visit(context);
    }

    private readonly record struct ProfileInfo(
        Antlr4.Runtime.Atn.DecisionInfo DecisionInfo,
        string RuleName)
    {
        /// <inheritdoc />
        public override string ToString()
            => $"{RuleName}: time={DecisionInfo.timeInPrediction} errors={DecisionInfo.errors.Count} " +
               $"ambiguities={DecisionInfo.ambiguities.Count}";
    }

    private void DumpProfileInfo()
    {
        var info = _parser.ParseInfo.getDecisionInfo()
            .OrderByDescending(di => di.timeInPrediction)
            .Select(di => new ProfileInfo(
                di,
                _parser.RuleNames[_parser.Atn.GetDecisionState(di.decision).ruleIndex]))
            .ToList();
    }

    private static IReadOnlyList<IAstBuilder.Token> TransformTokens(IList<IToken> tokens)
    {
        var result = new List<IAstBuilder.Token>(capacity: tokens.Count);

        foreach (var token in tokens)
        {
            if (token.Type == QueryCatParser.Eof)
            {
                continue;
            }

            // Skip comments.
            if (token.Type is QueryCatLexer.SINGLE_LINE_COMMENT or QueryCatLexer.MULTILINE_COMMENT)
            {
                continue;
            }

            if (token.Type == QueryCatParser.QUOTES_IDENTIFIER)
            {
                result.Add(new IAstBuilder.Token(
                    StringUtils.Unquote(token.Text),
                    ParserToken.TokenKindIdentifier,
                    token.StartIndex)
                );
            }
            else if (token.Type == QueryCatParser.NO_QUOTES_IDENTIFIER)
            {
                result.Add(new IAstBuilder.Token(token.Text, ParserToken.TokenKindIdentifier, token.StartIndex));
            }
            else
            {
                result.Add(new IAstBuilder.Token(token.Text,
                    QueryCatParser.DefaultVocabulary.GetSymbolicName(token.Type), token.StartIndex));
            }
        }

        return result;
    }
}
