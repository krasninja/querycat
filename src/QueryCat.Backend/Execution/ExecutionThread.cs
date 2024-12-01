using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution thread that includes statements to be executed, local variables, options and statistic.
/// </summary>
public class ExecutionThread : IExecutionThread<ExecutionOptions>
{
    internal const string ApplicationDirectory = "qcat";
    internal const string BootstrapFileName = "rc.sql";

    private readonly AstVisitor _statementsVisitor;
    private readonly SemaphoreSlim _semaphore;
    private int _deepLevel;
    private readonly List<IDisposable> _disposablesList = new();
    private bool _isInCallback;

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; }

    private IExecutionScope _topScope;
    private readonly IExecutionScope _rootScope;
    private bool _bootstrapScriptExecuted;
    private bool _configLoaded;
    private readonly Stopwatch _stopwatch = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ExecutionThread));

    /// <summary>
    /// Root (base) thread scope.
    /// </summary>
    internal IExecutionScope RootScope => _rootScope;

    /// <summary>
    /// AST builder.
    /// </summary>
    internal IAstBuilder AstBuilder { get; }

    /// <inheritdoc />
    public IExecutionScope TopScope => _topScope;

    /// <inheritdoc />
    public IExecutionStack Stack { get; } = new DefaultFixedSizeExecutionStack();

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolving;

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolved;

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
    /// Last execution statement return value.
    /// </summary>
    public VariantValue LastResult { get; private set; } = VariantValue.Null;

    /// <summary>
    /// The event to be called before any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? StatementExecuting;

    /// <summary>
    /// The event to be called after any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? StatementExecuted;

    /// <summary>
    /// Get application directory to store local data.
    /// </summary>
    /// <param name="ensureExists">Create the directory if it doesn't exist.</param>
    /// <returns>Default application directory.</returns>
    public static string GetApplicationDirectory(bool ensureExists = false)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDirectory);
        if (ensureExists)
        {
            Directory.CreateDirectory(directory);
        }
        return directory;
    }

    internal ExecutionThread(
        ExecutionOptions options,
        IFunctionsManager functionsManager,
        IObjectSelector objectSelector,
        IInputConfigStorage configStorage,
        IAstBuilder astBuilder,
        ICompletionSource completionSource,
        object? tag = null)
    {
        Options = options;
        FunctionsManager = functionsManager;
        ObjectSelector = objectSelector;
        ConfigStorage = configStorage;
        AstBuilder = astBuilder;
        CompletionSource = completionSource;
        Tag = tag;
        _statementsVisitor = new StatementsVisitor(this);

        _rootScope = new ExecutionScope(parent: null);
        _topScope = _rootScope;
        _semaphore = new SemaphoreSlim(Options.ConcurrencyLevel, Options.ConcurrencyLevel);
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="executionThread">Execution thread to copy from.</param>
    public ExecutionThread(ExecutionThread executionThread) :
        this(executionThread.Options,
            executionThread.FunctionsManager,
            executionThread.ObjectSelector,
            executionThread.ConfigStorage,
            executionThread.AstBuilder,
            executionThread.CompletionSource,
            executionThread.Tag)
    {
        _rootScope = new ExecutionScope(parent: null);
#if ENABLE_PLUGINS
        PluginsManager = executionThread.PluginsManager;
#endif
        _statementsVisitor = executionThread._statementsVisitor;
    }

    /// <inheritdoc />
    public virtual VariantValue Run(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            return VariantValue.Null;
        }

        // Run with lock and timer.
        _semaphore.Wait(cancellationToken);
        try
        {
            CurrentQuery = query;
            var programNode = AstBuilder.BuildProgramFromString(query);

            // Bootstrap.
            _deepLevel++;
            if (_deepLevel == 1)
            {
                RunBootstrapScript();
                LoadConfig();
            }
            if (_deepLevel > Options.MaxRecursionDepth)
            {
                throw new QueryCatException(
                    string.Format(Resources.Errors.ExecutionMaxRecursionDepth, Options.MaxRecursionDepth));
            }

            // Set first executing statement and run.
            var executingStatement = programNode.Statements.FirstOrDefault();

            // Setup timer.
            if (_deepLevel == 1)
            {
                _stopwatch.Restart();
            }

            if (executingStatement == null)
            {
                return VariantValue.Null;
            }
            if (parameters != null)
            {
                var scope = PushScope();
                SetScopeVariables(scope, parameters);
            }
            return Options.QueryTimeout != TimeSpan.Zero
                ? RunWithTimeout(ct => RunInternal(executingStatement, ct), cancellationToken)
                : RunInternal(executingStatement, cancellationToken);
        }
        finally
        {
            if (parameters != null)
            {
                PopScope();
            }
            if (_deepLevel == 1)
            {
                _stopwatch.Stop();
                Statistic.ExecutionTime = _stopwatch.Elapsed;
            }
            _deepLevel--;
            CurrentQuery = string.Empty;

            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public IExecutionScope PushScope()
    {
        var scope = new ExecutionScope(TopScope);
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

    private T RunWithTimeout<T>(Func<CancellationToken, T> func, CancellationToken cancellationToken)
    {
        var task = Task.Run(() => func(cancellationToken), cancellationToken);
        if (task.IsCompleted)
        {
            return task.Result;
        }
        return task.WaitAsync(Options.QueryTimeout, cancellationToken).GetAwaiter().GetResult();
    }

    private VariantValue RunInternal(StatementNode executingStatement, CancellationToken cancellationToken)
    {
        var executeEventArgs = new ExecuteEventArgs(executingStatement);
        StatementNode? currentStatement = executingStatement;
        while (currentStatement != null)
        {
            executeEventArgs.ExecutingStatementNode = currentStatement;

            // Evaluate the command.
            var commandContext = _statementsVisitor.RunAndReturn(currentStatement);
            if (commandContext is IDisposable disposable)
            {
                _disposablesList.Add(disposable);
            }

            // Fire "before" event.
            if (StatementExecuting != null && !_isInCallback)
            {
                _isInCallback = true;
                StatementExecuting.Invoke(this, executeEventArgs);
                _isInCallback = false;
            }
            if (!executeEventArgs.ContinueExecution)
            {
                break;
            }

            // Run the command.
            LastResult = commandContext.Invoke(this);

            // Fire "after" event.
            if (StatementExecuted != null && !_isInCallback)
            {
                _isInCallback = true;
                StatementExecuted.Invoke(this, executeEventArgs);
                _isInCallback = false;
            }
            if (!executeEventArgs.ContinueExecution)
            {
                break;
            }

            // Write result.
            if (Options.DefaultRowsOutput != NullRowsOutput.Instance)
            {
                Write(LastResult, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Get the next statement to execute.
            currentStatement = currentStatement.NextNode;
        }

        if (Options.UseConfig)
        {
            AsyncUtils.RunSync(ConfigStorage.SaveAsync);
        }

        return LastResult;
    }

    /// <summary>
    /// Dumps current executing AST statement.
    /// </summary>
    /// <returns>AST string.</returns>
    public string DumpAst(ExecuteEventArgs args)
    {
        var sb = new StringBuilder();
        var visitor = new StringDumpAstVisitor(sb);
        visitor.Run(args.ExecutingStatementNode);
        return sb.ToString();
    }

    #region Variables

    /// <inheritdoc />
    public virtual bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null)
    {
        var eventArgs = new ResolveVariableEventArgs(name, this);

        VariableResolving?.Invoke(this, eventArgs);
        if (eventArgs.Handled)
        {
            value = eventArgs.Result;
            return true;
        }
        name = eventArgs.VariableName;

        var currentScope = TopScope;
        while (currentScope != null)
        {
            if (currentScope.Variables.TryGetValue(name, out value))
            {
                eventArgs.Handled = true;
                eventArgs.Result = value;
                VariableResolved?.Invoke(this, eventArgs);
                if (eventArgs.Handled)
                {
                    value = eventArgs.Result;
                    return true;
                }

                value = VariantValue.Null;
                return false;
            }
            currentScope = currentScope.Parent;
        }

        eventArgs.Handled = false;
        VariableResolved?.Invoke(this, eventArgs);
        if (eventArgs.Handled)
        {
            value = eventArgs.Result;
            return true;
        }

        value = VariantValue.Null;
        return false;
    }

    #endregion

    /// <inheritdoc />
    public IEnumerable<CompletionResult> GetCompletions(string text, int position = -1, object? tag = null)
    {
        var tokens = AstBuilder
            .GetTokens(text)
            .Select(t => new ParserToken(t.Text, t.Type, t.StartIndex))
            .ToList();
        var context = new CompletionContext(this, text, tokens, position);
        context.Tag = tag;
        return CompletionSource.Get(context).OrderByDescending(c => c.Completion.Relevance);
    }

    private void Write(VariantValue result, CancellationToken cancellationToken)
    {
        if (result.IsNull)
        {
            return;
        }
        var iterator = ExecutionThreadUtils.ConvertToIterator(result);
        var rowsOutput = Options.DefaultRowsOutput;
        if (result.Type == DataType.Object
            && result.AsObjectUnsafe is IRowsOutput alternateRowsOutput)
        {
            rowsOutput = alternateRowsOutput;
        }
        rowsOutput.Reset();
        Write(rowsOutput, iterator, cancellationToken);
    }

    private void Write(
        IRowsOutput rowsOutput,
        IRowsIterator rowsIterator,
        CancellationToken cancellationToken)
    {
        // For plain output let's adjust columns width first.
        if (rowsOutput.Options.RequiresColumnsLengthAdjust && Options.AnalyzeRowsCount > 0)
        {
            rowsIterator = new AdjustColumnsLengthsIterator(rowsIterator, Options.AnalyzeRowsCount);
        }
        if (Options.TailCount > -1)
        {
            rowsIterator = new TailRowsIterator(rowsIterator, Options.TailCount);
        }

        // Write the main data.
        var isOpened = false;
        StartWriterLoop();

        // Append grow data.
        if (Options.FollowTimeout != TimeSpan.Zero)
        {
            while (true)
            {
                var requestQuit = false;
                Thread.Sleep(Options.FollowTimeout);
                StartWriterLoop();
                ProcessInput(ref requestQuit);
                if (cancellationToken.IsCancellationRequested || requestQuit)
                {
                    break;
                }
            }
        }

        if (isOpened)
        {
            rowsOutput.Close();
        }

        void StartWriterLoop()
        {
            while (rowsIterator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (!isOpened)
                {
                    rowsOutput.Open();
                    rowsOutput.QueryContext = new RowsOutputQueryContext(rowsIterator.Columns);
                    isOpened = true;
                }
                rowsOutput.WriteValues(rowsIterator.Current.Values);
            }
        }

        void ProcessInput(ref bool requestQuit)
        {
            if (!Environment.UserInteractive)
            {
                return;
            }
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
                else if (key.Key == ConsoleKey.Q)
                {
                    requestQuit = true;
                }
                else if (key.Key == ConsoleKey.Subtract)
                {
                    Console.WriteLine(new string('-', 5));
                }
            }
        }
    }

    private void LoadConfig()
    {
        if (!_configLoaded && Options.UseConfig)
        {
            AsyncUtils.RunSync(ConfigStorage.LoadAsync);
            _configLoaded = true;
        }
    }

    private void RunBootstrapScript()
    {
        if (!_bootstrapScriptExecuted && Options.RunBootstrapScript)
        {
            _bootstrapScriptExecuted = true;

            var rcFile = Path.Combine(GetApplicationDirectory(), BootstrapFileName);
            if (File.Exists(rcFile))
            {
                Run(File.ReadAllText(rcFile));
            }

            rcFile = Path.Combine(Directory.GetCurrentDirectory(), BootstrapFileName);
            if (File.Exists(rcFile))
            {
                Run(File.ReadAllText(rcFile));
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore.Dispose();
            foreach (var disposable in _disposablesList)
            {
                disposable.Dispose();
            }
            _disposablesList.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
