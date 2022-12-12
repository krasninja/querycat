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

    private readonly StatementsVisitor _statementsVisitor;

    internal PersistentInputConfigStorage InputConfigStorage { get; }

    /// <summary>
    /// Configuration storage.
    /// </summary>
    public IInputConfigStorage ConfigStorage => InputConfigStorage;

    /// <summary>
    /// Root scope.
    /// </summary>
    internal ExecutionScope RootScope { get; }

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
        var result = RunInternal();
        ExecutingStatement = null;
        return result;
    }

    /// <summary>
    /// Run the execution flow.
    /// </summary>
    internal VariantValue RunInternal()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
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
                    Options.DefaultRowsOutput.Write(result);
                }
            }
            finally
            {
                commandContext.Dispose();
            }

            ExecutingStatement = ExecutingStatement.Next;
        }

        if (Options.UseConfig)
        {
            InputConfigStorage.SaveAsync().GetAwaiter().GetResult();
        }
        stopwatch.Stop();
        Statistic.ExecutionTime = stopwatch.Elapsed;
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
    /// <param name="function">Function instance.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Return value.</returns>
    public VariantValue CallFunction(Function function, params object[] args)
    {
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(this, args);
        return function.Delegate.Invoke(functionCallInfo);
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
}
