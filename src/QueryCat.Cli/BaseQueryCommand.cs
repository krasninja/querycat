using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Providers;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

/// <summary>
/// Base class for CLI commands.
/// </summary>
public abstract class BaseQueryCommand
{
    [Argument(0, Description = "SQL-like query.")]
    public string Query { get; } = string.Empty;

    [Option("--row-number", Description = "Include row number column.")]
    public bool RowNumber { get; } = false;

    [Option("--page-size", Description = "Output page size. Set -1 to show all.")]
    public int PageSize { get; } = 20;

    [Option("--log-level", Description = "Log level.")]
    public LogLevel LogLevel { get; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Info;
#endif

    [Option("--plugin-dir", Description = "Plugin directory.")]
    public string[] PluginDirectories { get; } = Array.Empty<string>();

    /// <summary>
    /// Command entry point.
    /// </summary>
    /// <param name="app">Application instance.</param>
    /// <param name="console">Console abstraction.</param>
    /// <returns>Error code.</returns>
    public virtual int OnExecute(CommandLineApplication app, IConsole console)
    {
        Logger.Instance.MinLevel = LogLevel;
        return 0;
    }

    protected Runner CreateRunner()
    {
        var executionOptions = new ExecutionOptions
        {
            AddRowNumberColumn = RowNumber,
            PagingSize = PageSize,
            PluginAssemblies = { typeof(QueryCat.DataProviders.Registration).Assembly }
        };
        executionOptions.DefaultRowsOutput = ConsoleDataProviders.CreateConsole(null, pageSize: executionOptions.PagingSize);
        var pluginLoader = new PluginsLoader(PluginDirectories);
        executionOptions.PluginAssemblies.AddRange(pluginLoader.GetPlugins());
        var runner = new Runner(executionOptions);
        runner.Bootstrap();
        return runner;
    }
}
