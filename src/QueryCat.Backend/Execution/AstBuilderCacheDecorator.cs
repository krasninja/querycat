using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Execution;

internal sealed class AstBuilderCacheDecorator : IAstBuilder
{
    internal const int DefaultMaxQueryLengthForCache = 150;

    private readonly IAstBuilder _astBuilder;

    private readonly IDictionary<string, IAstNode> _astCache;
    private readonly int _maxQueryLengthForCache;

    /// <summary>
    /// Use cache.
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="astBuilder">Instance of <see cref="IAstBuilder" />.</param>
    /// <param name="cache">Cache for AST.</param>
    /// <param name="maxQueryLengthForCache">Max query length for cache.</param>
    public AstBuilderCacheDecorator(
        IAstBuilder astBuilder,
        IDictionary<string, IAstNode>? cache = null,
        int maxQueryLengthForCache = DefaultMaxQueryLengthForCache)
    {
        _astBuilder = astBuilder;
        _astCache = cache ?? new Dictionary<string, IAstNode>();
        _maxQueryLengthForCache = maxQueryLengthForCache;
    }

    /// <inheritdoc />
    public ProgramNode BuildProgramFromString(string program)
    {
        // Cache only small queries.
        if (!EnableCache || program.Length > _maxQueryLengthForCache)
        {
            return _astBuilder.BuildProgramFromString(program);
        }

        if (_astCache.TryGetValue(program, out var resultNode))
        {
            return (ProgramNode)resultNode.Clone();
        }

        resultNode = _astBuilder.BuildProgramFromString(program);
        _astCache[program] = resultNode;
        return (ProgramNode)resultNode;
    }

    /// <inheritdoc />
    public FunctionSignatureNode BuildFunctionSignatureFromString(string function)
        => _astBuilder.BuildFunctionSignatureFromString(function);

    /// <inheritdoc />
    public IAstBuilder.Token[] GetTokens(string text) => _astBuilder.GetTokens(text);
}
