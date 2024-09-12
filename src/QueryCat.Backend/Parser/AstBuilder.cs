using Antlr4.Runtime;
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

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="cache">Cache for AST.</param>
    /// <param name="maxQueryLength">Max query length for cache.</param>
    public AstBuilder(IDictionary<string, IAstNode>? cache = null, int maxQueryLength = DefaultMaxQueryLengthForCache)
    {
        _astCache = cache;
        _maxQueryLength = maxQueryLength;
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
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        commonTokenStream.Fill();

        return TransformTokens(commonTokenStream.GetTokens()).ToList();
    }

    private TNode Build<TNode>(
        string input,
        Func<QueryCatParser, ParserRuleContext> signatureFunc) where TNode : IAstNode
    {
        // Cache only small queries.
        if (_astCache == null || input.Length > _maxQueryLength)
        {
            return BuildInternal<TNode>(input, signatureFunc);
        }

        if (_astCache != null && _astCache.TryGetValue(input, out var resultNode))
        {
            return (TNode)resultNode.Clone();
        }

        resultNode = BuildInternal<TNode>(input, signatureFunc);
        if (_astCache != null)
        {
            _astCache[input] = (IAstNode)resultNode.Clone();
        }
#if DEBUG
        // Return cloned node instead for debug only purposes.
        return (TNode)resultNode.Clone();
#else
        return (TNode)resultNode;
#endif
    }

    private static TNode BuildInternal<TNode>(string input, Func<QueryCatParser, ParserRuleContext> signatureFunc)
        where TNode : IAstNode
    {
        var errorListener = new ProgramAntlrErrorListener();

        var inputStream = new AntlrInputStream(input);
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new QueryCatParser(commonTokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);
        parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
        var context = signatureFunc.Invoke(parser);
        var visitor = new ProgramParserVisitor();
        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new SyntaxException(errorListener.Message, input, errorListener.Line, errorListener.CharPosition);
        }

        return (TNode)visitor.Visit(context);
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
