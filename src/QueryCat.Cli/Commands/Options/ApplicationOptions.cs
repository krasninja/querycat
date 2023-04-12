using Serilog;
using Serilog.Events;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Providers;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Main/shared command line options.
/// </summary>
internal class ApplicationOptions
{
    public LogEventLevel LogLevel { get; init; }

#if ENABLE_PLUGINS
    public string[] PluginDirectories { get; init; } = Array.Empty<string>();
#endif

    public ExecutionThread CreateExecutionThread(ExecutionOptions? executionOptions = null)
    {
        executionOptions ??= new ExecutionOptions
        {
            RunBootstrapScript = true,
            UseConfig = true,
        };
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
#endif
        var executionThread = new ExecutionThread(executionOptions);
        executionThread.Statistic.CountErrorRows = executionThread.Options.ShowDetailedStatistic;
        new ExecutionThreadBootstrapper().Bootstrap(executionThread);
        return executionThread;
    }

    public ExecutionThread CreateStdoutExecutionThread(
        ExecutionOptions? executionOptions = null,
        string? columnsSeparator = null,
        TextTableOutput.Style outputStyle = TextTableOutput.Style.Table)
    {
        var thread = CreateExecutionThread(executionOptions);
        var tableOutput = new TextTableOutput(
            stream: StandardInputOutput.GetConsoleOutput(),
            separator: columnsSeparator,
            style: outputStyle);
        thread.Options.DefaultRowsOutput = new PagingOutput(
            tableOutput, pagingRowsCount: PagingOutput.NoLimit);
        return thread;
    }

    /// <summary>
    /// Pre initialization step for logger.
    /// </summary>
    public void InitializeLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogLevel)
            .WriteTo.Console()
            .CreateLogger();
    }
}
