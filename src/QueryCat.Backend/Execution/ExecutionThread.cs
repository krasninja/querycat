using System.Diagnostics;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Execution.Plugins;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution thread that includes statements to be executed, local variables, options and statistic.
/// </summary>
public sealed class ExecutionThread
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

    /// <summary>
    /// Plugins manager.
    /// </summary>
    public PluginsManager PluginsManager { get; }

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
        PluginsManager = new PluginsManager(
            PluginsManager.GetPluginDirectories(appLocalDirectory).Union(Options.PluginDirectories));
        _statementsVisitor = new StatementsVisitor(this);
        InputConfigStorage = new PersistentInputConfigStorage(Path.Combine(appLocalDirectory, ConfigFileName));
    }

    /// <summary>
    /// Run the execution flow.
    /// </summary>
    public VariantValue Run()
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

    internal void LoadPlugins()
    {
        var pluginLoader = new PluginsLoader(PluginsManager.PluginDirectories);
        Options.PluginAssemblies.AddRange(pluginLoader.LoadPlugins());

        foreach (var pluginAssembly in Options.PluginAssemblies)
        {
            FunctionsManager.RegisterFromAssembly(pluginAssembly);
        }
    }
}
