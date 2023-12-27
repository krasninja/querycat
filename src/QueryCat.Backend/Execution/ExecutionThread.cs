using System.Diagnostics;
using System.Text;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Parser;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution thread that includes statements to be executed, local variables, options and statistic.
/// </summary>
public class ExecutionThread : IExecutionThread
{
    internal const string ApplicationDirectory = "qcat";
    internal const string BootstrapFileName = "rc.sql";

    private readonly StatementsVisitor _statementsVisitor;
    private readonly object _objLock = new();
    private readonly List<IDisposable> _disposablesList = new();
    private bool _isInCallback;

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; }

    /// <summary>
    /// Root (base) thread scope.
    /// </summary>
    internal ExecutionScope RootScope { get; }

    /// <inheritdoc />
    public IExecutionScope TopScope => RootScope;

    /// <summary>
    /// Current executing statement.
    /// </summary>
    internal StatementNode? ExecutingStatement { get; set; }

    /// <summary>
    /// Execution options.
    /// </summary>
    public ExecutionOptions Options { get; }

    /// <summary>
    /// Statistic.
    /// </summary>
    public ExecutionStatistic Statistic { get; } = new();

    /// <inheritdoc />
    public IFunctionsManager FunctionsManager { get; }

    /// <inheritdoc />
    public IPluginsManager PluginsManager { get; set; } = NullPluginsManager.Instance;

    /// <summary>
    /// Last execution statement return value.
    /// </summary>
    public VariantValue LastResult { get; private set; } = VariantValue.Null;

    /// <summary>
    /// The event to be called before any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? BeforeStatementExecute;

    /// <summary>
    /// The event to be called after any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? AfterStatementExecute;

    public static string GetApplicationDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDirectory);
    }

    internal ExecutionThread(
        ExecutionOptions options,
        IFunctionsManager functionsManager,
        IInputConfigStorage configStorage)
    {
        var appLocalDirectory = GetApplicationDirectory();
        RootScope = new ExecutionScope();
        Options = options;
        FunctionsManager = functionsManager;
        _statementsVisitor = new StatementsVisitor(this);
        ConfigStorage = configStorage;
        RunBootstrapScript(appLocalDirectory);
    }

    public ExecutionThread(ExecutionThread executionThread)
    {
        RootScope = new ExecutionScope();
        Options = executionThread.Options;
#if ENABLE_PLUGINS
        PluginsManager = executionThread.PluginsManager;
#endif
        _statementsVisitor = executionThread._statementsVisitor;
        FunctionsManager = executionThread.FunctionsManager;
        ConfigStorage = executionThread.ConfigStorage;
    }

    /// <inheritdoc />
    public VariantValue Run(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            return VariantValue.Null;
        }

        var programNode = AstBuilder.BuildProgramFromString(query);

        // Set first executing statement and run.
        ExecutingStatement = programNode.Statements.FirstOrDefault();

        // Run with lock and timer.
        lock (_objLock)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                return RunInternal(cancellationToken);
            }
            finally
            {
                stopwatch.Stop();
                Statistic.ExecutionTime = stopwatch.Elapsed;
            }
        }
    }

    private VariantValue RunInternal(CancellationToken cancellationToken)
    {
        if (Options.UseConfig)
        {
            AsyncUtils.RunSync(ConfigStorage.LoadAsync);
        }

        var executeEventArgs = new ExecuteEventArgs();
        while (ExecutingStatement != null)
        {
            var commandContext = _statementsVisitor.RunAndReturn(ExecutingStatement);
            _disposablesList.Add(commandContext);

            // Fire "before" event.
            if (BeforeStatementExecute != null && !_isInCallback)
            {
                _isInCallback = true;
                BeforeStatementExecute.Invoke(this, executeEventArgs);
                _isInCallback = false;
            }
            if (!executeEventArgs.ContinueExecution)
            {
                break;
            }

            LastResult = commandContext.Invoke();

            // Fire "after" event.
            if (AfterStatementExecute != null && !_isInCallback)
            {
                _isInCallback = true;
                AfterStatementExecute.Invoke(this, executeEventArgs);
                _isInCallback = false;
            }
            if (!executeEventArgs.ContinueExecution)
            {
                break;
            }

            if (Options.DefaultRowsOutput != NullRowsOutput.Instance)
            {
                Write(LastResult, cancellationToken);
            }

            ExecutingStatement = ExecutingStatement.NextNode;
        }

        if (Options.UseConfig)
        {
            AsyncUtils.RunSync(ConfigStorage.SaveAsync);
        }
        ExecutingStatement = null;

        return LastResult;
    }

    /// <summary>
    /// Dumps current executing AST statement.
    /// </summary>
    /// <returns>AST string.</returns>
    public string DumpAst()
    {
        if (ExecutingStatement == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var visitor = new StringDumpAstVisitor(sb);
        visitor.Run(ExecutingStatement);
        return sb.ToString();
    }

    private void Write(VariantValue result, CancellationToken cancellationToken)
    {
        if (result.IsNull)
        {
            return;
        }
        var iterator = ExecutionThreadUtils.ConvertToIterator(result);
        var rowsOutput = Options.DefaultRowsOutput;
        if (result.GetInternalType() == DataType.Object
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
        CancellationToken cancellationToken = default)
    {
        // For plain output let's adjust columns width first.
        if (rowsOutput.Options.RequiresColumnsLengthAdjust && Options.AnalyzeRowsCount > 0)
        {
            rowsIterator = new AdjustColumnsLengthsIterator(rowsIterator, Options.AnalyzeRowsCount);
        }

        // Write the main data.
        var isOpened = false;
        StartWriterLoop();

        // Append grow data.
        if (Options.FollowTimeoutMs > 0)
        {
            while (true)
            {
                Thread.Sleep(Options.FollowTimeoutMs);
                StartWriterLoop();
                if (cancellationToken.IsCancellationRequested)
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
    }

    private void RunBootstrapScript(string appLocalDirectory)
    {
        var rcFile = Path.Combine(appLocalDirectory, BootstrapFileName);
        if (Options.RunBootstrapScript && File.Exists(rcFile))
        {
            Run(File.ReadAllText(rcFile));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
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
