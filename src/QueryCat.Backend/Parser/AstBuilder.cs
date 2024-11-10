using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
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
    private const int DefaultMaxQueryLengthForCache = 150;

    private readonly IDictionary<string, IAstNode>? _astCache;
    private readonly int _maxQueryLength;

    private readonly QueryCatLexer _lexer = new(new AntlrInputStream(string.Empty), TextWriter.Null, TextWriter.Null);
    private readonly QueryCatParser _parser;
    private readonly ProgramAntlrErrorListener _errorListener = new();
    private readonly ProgramParserVisitor _programParserVisitor = new();

    /// <summary>
    /// Collect profile information. Use DumpProfileInfo() method.
    /// </summary>
    public bool ProfileMode { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="cache">Cache for AST.</param>
    /// <param name="maxQueryLength">Max query length for cache.</param>
    public AstBuilder(IDictionary<string, IAstNode>? cache = null, int maxQueryLength = DefaultMaxQueryLengthForCache)
    {
        _astCache = cache;
        _maxQueryLength = maxQueryLength;

        _parser = new QueryCatParser(new CommonTokenStream(_lexer));
        _parser.RemoveErrorListeners();
        _parser.AddErrorListener(_errorListener);
        _parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
    }

    /// <inheritdoc />
    public ProgramNode BuildProgramFromString(string program) => Build<ProgramNode>(program, p => p.program(), _astCache);

    /// <inheritdoc />
    public FunctionSignatureNode BuildFunctionSignatureFromString(string function)
        => Build<FunctionSignatureNode>(function, p => p.functionSignature(), astCache: null);

    /// <inheritdoc />
    public IReadOnlyList<IAstBuilder.Token> GetTokens(string text)
    {
        var inputStream = new AntlrInputStream(text);
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        commonTokenStream.Fill();

        return TransformTokens(commonTokenStream.GetTokens()).ToList();
    }

    private TNode Build<TNode>(
        string input,
        Func<QueryCatParser, ParserRuleContext> signatureFunc,
        IDictionary<string, IAstNode>? astCache) where TNode : IAstNode
    {
        // Cache only small queries.
        if (astCache == null || input.Length > _maxQueryLength)
        {
            return BuildInternal<TNode>(input, signatureFunc);
        }

        if (astCache.TryGetValue(input, out var resultNode))
        {
            return (TNode)resultNode.Clone();
        }

        resultNode = BuildInternal<TNode>(input, signatureFunc);
        astCache[input] = resultNode;
#if DEBUG
        // Return cloned node instead for debug only purposes.
        return (TNode)resultNode.Clone();
#else
        return (TNode)resultNode;
#endif
    }

    private TNode BuildInternal<TNode>(string input, Func<QueryCatParser, ParserRuleContext> signatureFunc)
        where TNode : IAstNode
    {
        _parser.Profile = ProfileMode;
        _lexer.SetInputStream(new AntlrInputStream(input));
        _parser.TokenStream = new CommonTokenStream(_lexer);
        var context = signatureFunc.Invoke(_parser);
        if (_parser.NumberOfSyntaxErrors > 0)
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
        DecisionInfo DecisionInfo,
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

    private static IEnumerable<IAstBuilder.Token> TransformTokens(IEnumerable<IToken> tokens)
    {
        foreach (var token in tokens)
        {
            if (token.Type == QueryCatParser.Eof)
            {
                continue;
            }

            if (token.Type == QueryCatParser.QUOTES_IDENTIFIER)
            {
                yield return new IAstBuilder.Token(
                    StringUtils.Unquote(token.Text).ToString(),
                    ParserToken.TokenKindIdentifier,
                    token.StartIndex);
            }
            else if (token.Type == QueryCatParser.NO_QUOTES_IDENTIFIER)
            {
                yield return new IAstBuilder.Token(token.Text, ParserToken.TokenKindIdentifier, token.StartIndex);
            }
            else
            {
                yield return new IAstBuilder.Token(token.Text,
                    QueryCatParser.DefaultVocabulary.GetSymbolicName(token.Type), token.StartIndex);
            }
        }
    }
}
