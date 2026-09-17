using System.Text;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
#if NET8_0 || NET9_0
using QueryCat.Backend.Core.Utils;
#endif
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution thread that includes statements to be executed, local variables, options and statistic.
/// </summary>
public class DefaultExecutionThread : IExecutionThread<ExecutionOptions>, IExecutionThreadPrepare, IAsyncDisposable
{
    private const string BootstrapFileName = "rc.sql";

    private readonly Func<IExecutionScope?, IExecutionScope> _executionScopeFactory;
    private int _deepLevel;
    private bool _isDisposed;

    /// <inheritdoc />
    public IConfigStorage ConfigStorage { get; }

    private IExecutionScope _topScope;
    private bool _bootstrapScriptExecuted;
    private bool _configLoaded;
    private readonly AsyncLock _asyncLock = new();

    private bool IsInCallback { get; set; }

    public Func<VariantValue, CancellationToken, ValueTask>? CommandResultOutput { get; set; }

    private sealed class DefaultBodyFuncUnit : StatementsBlockFuncUnit
    {
        /// <inheritdoc />
        public DefaultBodyFuncUnit(DefaultExecutionThread executionThread, ProgramBodyNode programBodyNode)
            : base(new StatementsVisitor(executionThread), programBodyNode.Statements)
        {
        }

        /// <inheritdoc />
        protected override async ValueTask<VariantValue> InvokeStatementAsync(
            IExecutionThread thread,
            IFuncUnit funcUnit,
            StatementNode statementNode,
            CancellationToken cancellationToken = default)
        {
            var executionThread = (DefaultExecutionThread)thread;
            var statementExecuting = executionThread.StatementExecuting;
            var statementExecuted = executionThread.StatementExecuted;

            // Before.
            if (statementExecuting != null && !executionThread.IsInCallback)
            {
                executionThread.IsInCallback = true;
                var executeEventArgs = new ExecuteEventArgs(statementNode);
                try
                {
                    statementExecuting.Invoke(executionThread, executeEventArgs);
                }
                finally
                {
                    executionThread.IsInCallback = false;
                }
                if (!executeEventArgs.ContinueExecution)
                {
                    Jump = ExecutionJump.Halt;
                    return VariantValue.Null;
                }
            }

            // Invoke.
            var result = await base.InvokeStatementAsync(thread, funcUnit, statementNode, cancellationToken);

            // After.
            if (statementExecuted != null && !executionThread.IsInCallback)
            {
                executionThread.IsInCallback = true;
                var executeEventArgs = new ExecuteEventArgs(statementNode);
                executeEventArgs.Result = result;
                try
                {
                    statementExecuted.Invoke(executionThread, executeEventArgs);
                }
                finally
                {
                    executionThread.IsInCallback = false;
                }
                if (!executeEventArgs.ContinueExecution)
                {
                    Jump = ExecutionJump.Halt;
                    return VariantValue.Null;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// AST builder.
    /// </summary>
    private IAstBuilder AstBuilder { get; }

    /// <inheritdoc />
    public IExecutionScope TopScope => _topScope;

    /// <inheritdoc />
    public IExecutionStack Stack { get; } = new DefaultFixedSizeExecutionStack();

    /// <inheritdoc />
    public IObjectSelector ObjectSelector { get; protected set; }

    /// <summary>
    /// Completion source to help user complete his input.
    /// </summary>
    internal ICompletionSource CompletionSource { get; }

    /// <inheritdoc />
    public string CurrentQuery { get; private set; } = string.Empty;

    /// <inheritdoc />
    public ExecutionOptions Options { get; }

    /// <inheritdoc />
    public ExecutionStatistic Statistic { get; } = new DefaultExecutionStatistic();

    /// <inheritdoc />
    public object? Tag { get; set; }

    /// <inheritdoc />
    public IFunctionsManager FunctionsManager { get; protected set; }

    /// <inheritdoc />
    public IPluginsManager PluginsManager { get; internal set; } = NullPluginsManager.Instance;

    /// <summary>
    /// The event to be called before any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? StatementExecuting;

    /// <summary>
    /// The event to be called after any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? StatementExecuted;

    internal DefaultExecutionThread(
        ExecutionOptions options,
        IFunctionsManager functionsManager,
        IObjectSelector objectSelector,
        IConfigStorage configStorage,
        IAstBuilder astBuilder,
        ICompletionSource completionSource,
        Func<IExecutionScope?, IExecutionScope>? executionScopeFactory = null,
        object? tag = null)
    {
        Options = options;
        FunctionsManager = functionsManager;
        ObjectSelector = objectSelector;
        ConfigStorage = configStorage;
        AstBuilder = astBuilder;
        CompletionSource = completionSource;
        Tag = tag;

        _executionScopeFactory = executionScopeFactory ?? (parent => new DefaultExecutionScope(parent));
        _topScope = _executionScopeFactory.Invoke(null);
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="executionThread">Execution thread to copy from.</param>
    public DefaultExecutionThread(DefaultExecutionThread executionThread) :
        this(executionThread.Options,
            executionThread.FunctionsManager,
            executionThread.ObjectSelector,
            executionThread.ConfigStorage,
            executionThread.AstBuilder,
            executionThread.CompletionSource,
            executionThread._executionScopeFactory,
            executionThread.Tag)
    {
#if ENABLE_PLUGINS
        PluginsManager = executionThread.PluginsManager;
#endif
    }

    /// <inheritdoc />
    public virtual async Task<VariantValue> RunAsync(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        // Run with lock and timer.
        IAsyncDisposable? @lock = null;
        var previousQuery = CurrentQuery;
        var depthIncremented = false;
        try
        {
            if (Options.PreventConcurrentRun)
            {
                @lock = await _asyncLock.LockAsync(cancellationToken);
            }
            _deepLevel++;
            depthIncremented = true;
            CurrentQuery = query;

            // Bootstrap.
            if (_deepLevel == 1)
            {
                await RunBootstrapScriptAsync(cancellationToken);
                await LoadConfigAsync(cancellationToken);
            }
            if (_deepLevel > Options.MaxRecursionDepth)
            {
                throw new QueryCatException(
                    string.Format(Resources.Errors.ExecutionMaxRecursionDepth, Options.MaxRecursionDepth));
            }

            return await RunInternalAsync(query, parameters, cancellationToken);
        }
        finally
        {
            if (depthIncremented)
            {
                if (_deepLevel == 1)
                {
                    Statistic.StopStopwatch();
                }
                _deepLevel--;
            }
            CurrentQuery = previousQuery;
            if (@lock != null)
            {
                await @lock.DisposeAsync();
            }
        }
    }

    /// <inheritdoc />
    public IExecutionScope PushScope()
    {
        var scope = _executionScopeFactory.Invoke(TopScope);
        _topScope = scope;
        return scope;
    }

    /// <inheritdoc />
    public IExecutionScope? PopScope()
    {
        if (_topScope.Parent == null)
        {
            return null;
        }
        var oldScope = _topScope;
        _topScope = _topScope.Parent;
        return oldScope;
    }

    private static void SetScopeVariables(IExecutionScope scope, IDictionary<string, VariantValue> parameters)
    {
        foreach (var parameter in parameters)
        {
            scope.Variables[parameter.Key] = parameter.Value;
        }
    }

    private async Task<VariantValue> RunInternalAsync(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            return VariantValue.Null;
        }

        var programNode = AstBuilder.BuildProgramFromString(query);

        // Setup timer.
        if (_deepLevel == 1)
        {
            Statistic.RestartStopwatch();
        }

        IExecutionScope? pushedScope = null;
        if (parameters != null && parameters.Count > 0)
        {
            pushedScope = PushScope();
            SetScopeVariables(pushedScope, parameters);
        }

        try
        {
            if (Options.QueryTimeout == TimeSpan.Zero)
            {
                return await ExecuteStatementAsync(programNode.Body, cancellationToken);
            }
            else
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(Options.QueryTimeout);
                try
                {
                    return await ExecuteStatementAsync(programNode.Body, cts.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(Resources.Errors.QueryTimeout);
                }
            }
        }
        finally
        {
            if (pushedScope != null)
            {
                PopScope();
            }
        }
    }

    private async Task<VariantValue> ExecuteStatementAsync(ProgramBodyNode bodyNode, CancellationToken cancellationToken)
    {
        var bodyFuncUnit = new DefaultBodyFuncUnit(this, bodyNode);
        var result = await bodyFuncUnit.InvokeAsync(this, cancellationToken);

        if (Options.UseConfig)
        {
            await ConfigStorage.SaveAsync(cancellationToken);
        }

        if (CommandResultOutput != null)
        {
            await CommandResultOutput.Invoke(result, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Dumps current executing AST statement.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AST string.</returns>
    public async Task<string> DumpAstAsync(ExecuteEventArgs args, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        var visitor = new StringDumpAstVisitor(sb);
        await visitor.RunAsync(args.ExecutingStatementNode, cancellationToken);
        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CompletionResult>> GetCompletionsAsync(
        string text,
        int position = -1,
        object? tag = null,
        CancellationToken cancellationToken = default)
    {
        var source = await CompletionSource.GetAsync(CreateCompletionContext(text, position, tag), cancellationToken)
            .ToListAsync(cancellationToken);
        return source.OrderByDescending(c => c.Completion.Relevance).ToList();
    }

    private CompletionContext CreateCompletionContext(string text, int position, object? tag)
    {
        var sourceTokens = AstBuilder.GetTokens(text);
        var tokens = new List<ParserToken>(sourceTokens.Count);
        foreach (var token in sourceTokens)
        {
            tokens.Add(new ParserToken(token.Text, token.Type, token.StartIndex));
        }
        return new CompletionContext(this, text, tokens, position)
        {
            Tag = tag,
        };
    }

    private async Task LoadConfigAsync(CancellationToken cancellationToken)
    {
        if (!_configLoaded && Options.UseConfig)
        {
            await ConfigStorage.LoadAsync(cancellationToken);
            _configLoaded = true;
        }
    }

    private async Task RunBootstrapScriptAsync(CancellationToken cancellationToken)
    {
        if (!_bootstrapScriptExecuted && Options.RunBootstrapScript)
        {
            _bootstrapScriptExecuted = true;

            var rcFile = Path.Combine(Application.GetApplicationDirectory(), BootstrapFileName);
            if (File.Exists(rcFile))
            {
                var query = await File.ReadAllTextAsync(rcFile, cancellationToken);
                await RunInternalAsync(query, cancellationToken: cancellationToken);
            }

            rcFile = Path.Combine(Directory.GetCurrentDirectory(), BootstrapFileName);
            if (File.Exists(rcFile))
            {
                var query = await File.ReadAllTextAsync(rcFile, cancellationToken);
                await RunInternalAsync(query, cancellationToken: cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public Func<CancellationToken, ValueTask<VariantValue>> Prepare(string query)
    {
        var programNode = AstBuilder.BuildProgramFromString(query);
        var bodyFuncUnit = new DefaultBodyFuncUnit(this, programNode.Body);

        return async ct =>
        {
            IAsyncDisposable? @lock = null;
            try
            {
                if (Options.PreventConcurrentRun)
                {
                    @lock = await _asyncLock.LockAsync(ct);
                }
                return await bodyFuncUnit.InvokeAsync(this, ct);
            }
            finally
            {
                if (@lock != null)
                {
                    await @lock.DisposeAsync();
                }
            }
        };
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        if (disposing)
        {
            _asyncLock.Dispose();
#if ENABLE_PLUGINS
            (PluginsManager as IDisposable)?.Dispose();
            (PluginsManager.PluginsLoader as IDisposable)?.Dispose();
#endif
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _asyncLock.DisposeAsync();
#if ENABLE_PLUGINS
        if (PluginsManager is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            (PluginsManager as IDisposable)?.Dispose();
        }
        (PluginsManager.PluginsLoader as IDisposable)?.Dispose();
#endif
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}
