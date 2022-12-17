using Serilog;
using Serilog.Events;
using QueryCat.Backend.Execution;

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
        executionOptions ??= new ExecutionOptions();
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
#endif
        var executionThread = new ExecutionThread(executionOptions);
        executionThread.Statistic.CountErrorRows = executionThread.Options.ShowDetailedStatistic;
        new ExecutionThreadBootstrapper().Bootstrap(executionThread);
        return executionThread;
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
