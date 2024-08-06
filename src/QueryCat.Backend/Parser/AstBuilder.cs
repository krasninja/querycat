using Antlr4.Runtime;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Parser;

/// <summary>
/// Build AST from query string.
/// </summary>
internal sealed class AstBuilder : IAstBuilder
{
    private const int MaxQueryLengthForCache = 150;

    private readonly IDictionary<string, IAstNode>? _astCache;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="cache">Cache for AST.</param>
    public AstBuilder(IDictionary<string, IAstNode>? cache = null)
    {
        _astCache = cache;
    }

    /// <inheritdoc />
    public ProgramNode BuildProgramFromString(string program)
        => Build<ProgramNode>(program, p => p.program());

    /// <inheritdoc />
    public FunctionSignatureNode BuildFunctionSignatureFromString(string function)
        => Build<FunctionSignatureNode>(function, p => p.functionSignature());

    private TNode Build<TNode>(
        string input,
        Func<QueryCatParser, ParserRuleContext> signatureFunc) where TNode : IAstNode
    {
        // Cache only small queries.
        if (input.Length > MaxQueryLengthForCache)
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
        var context = signatureFunc.Invoke(parser);
        var visitor = new ProgramParserVisitor();
        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new SyntaxException(errorListener.Message, input, errorListener.Line, errorListener.CharPosition);
        }

        return (TNode)visitor.Visit(context);
    }
}
