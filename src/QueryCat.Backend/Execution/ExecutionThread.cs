using System.Diagnostics;
using System.Text;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Execution.Plugins;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Parser;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution thread that includes statements to be executed, local variables, options and statistic.
/// </summary>
public class ExecutionThread : IExecutionThread
{
    internal const string ApplicationDirectory = "qcat";
    internal const string ConfigFileName = "config.json";
    internal const string BootstrapFileName = "rc.sql";

    private readonly StatementsVisitor _statementsVisitor;
    private readonly object _objLock = new();

    internal PersistentInputConfigStorage InputConfigStorage { get; }

    /// <summary>
    /// Configuration storage.
    /// </summary>
    public IInputConfigStorage ConfigStorage => InputConfigStorage;

    /// <summary>
    /// Root (base) thread scope.
    /// </summary>
    internal ExecutionScope RootScope { get; }

    /// <summary>
    /// Current top scope.
    /// </summary>
    internal ExecutionScope TopScope => RootScope;

    /// <summary>
    /// Functions manager.
    /// </summary>
    public FunctionsManager FunctionsManager { get; } = new();

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

#if ENABLE_PLUGINS
    /// <summary>
    /// Plugins manager.
    /// </summary>
    public PluginsManager PluginsManager { get; }
#endif

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

    public static readonly ExecutionThread Empty = new();

    internal static string GetApplicationDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDirectory);
    }

    public ExecutionThread(ExecutionOptions? options = null)
    {
        var appLocalDirectory = GetApplicationDirectory();

        RootScope = new ExecutionScope();
        Options = options ?? new ExecutionOptions();
#if ENABLE_PLUGINS
        PluginsManager = new PluginsManager(
            PluginsManager.GetPluginDirectories(appLocalDirectory).Union(Options.PluginDirectories),
            Options.PluginsRepositoryUri);
#endif
        _statementsVisitor = new StatementsVisitor(this);
        InputConfigStorage = new PersistentInputConfigStorage(Path.Combine(appLocalDirectory, ConfigFileName));
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
        InputConfigStorage = executionThread.InputConfigStorage;
    }

    /// <inheritdoc />
    public VariantValue Run(string query)
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
                return RunInternal();
            }
            finally
            {
                stopwatch.Stop();
                Statistic.ExecutionTime = stopwatch.Elapsed;
            }
        }
    }

    /// <summary>
    /// Run the execution flow.
    /// </summary>
    private VariantValue RunInternal()
    {
        if (Options.UseConfig)
        {
            InputConfigStorage.LoadAsync().GetAwaiter().GetResult();
        }

        while (ExecutingStatement != null)
        {
            var commandContext = _statementsVisitor.RunAndReturn(ExecutingStatement);

            try
            {
                // Fire "before" event.
                var executeEventArgs = new ExecuteEventArgs();
                BeforeStatementExecute?.Invoke(this, executeEventArgs);
                if (!executeEventArgs.ContinueExecution)
                {
                    break;
                }
                var result = commandContext.Invoke();
                LastResult = result;

                // Fire "after" event.
                AfterStatementExecute?.Invoke(this, executeEventArgs);
                if (!executeEventArgs.ContinueExecution)
                {
                    break;
                }

                if (Options.DefaultRowsOutput != NullRowsOutput.Instance)
                {
                    Write(result);
                }
            }
            finally
            {
                commandContext.Dispose();
            }

            ExecutingStatement = ExecutingStatement.NextNode;
        }

        if (Options.UseConfig)
        {
            InputConfigStorage.SaveAsync().GetAwaiter().GetResult();
        }
        ExecutingStatement = null;

        return LastResult;
    }

#if ENABLE_PLUGINS
    internal void LoadPlugins()
    {
        var pluginLoader = new PluginsLoader(PluginsManager.PluginDirectories);
        Options.PluginAssemblies.AddRange(pluginLoader.LoadPlugins());

        foreach (var pluginAssembly in Options.PluginAssemblies)
        {
            FunctionsManager.RegisterFromAssembly(pluginAssembly);
        }
    }
#endif

    /// <summary>
    /// Call function within execution thread.
    /// </summary>
    /// <param name="functionDelegate">Function delegate instance.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Return value.</returns>
    public VariantValue RunFunction(FunctionDelegate functionDelegate, params object[] args)
    {
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(this, args);
        return functionDelegate.Invoke(functionCallInfo);
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

    private void Write(VariantValue result)
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
        rowsOutput.Write(iterator);
    }

    private void RunBootstrapScript(string appLocalDirectory)
    {
        var rcFile = Path.Combine(appLocalDirectory, BootstrapFileName);
        if (File.Exists(rcFile))
        {
            Run(File.ReadAllText(rcFile));
        }
    }
}
