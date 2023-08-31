using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Providers;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Main/shared command line options.
/// </summary>
internal class ApplicationOptions
{
    internal const string ConfigFileName = "config.json";
    internal const string ApplicationPluginsDirectory = "plugins";

    public LogLevel LogLevel { get; init; }

#if ENABLE_PLUGINS
    public string[] PluginDirectories { get; init; } = Array.Empty<string>();
#endif

    public ApplicationRoot CreateApplicationRoot(ExecutionOptions? executionOptions = null)
    {
        executionOptions ??= new ExecutionOptions
        {
            RunBootstrapScript = true,
            UseConfig = true,
        };
        // ReSharper disable once RedundantAssignment
        PluginsLoader pluginsLoader = NullPluginsLoader.Instance;
        var storage = new PersistentInputConfigStorage(
            Path.Combine(ExecutionThread.GetApplicationDirectory(), ConfigFileName));
        var executionThread = new ExecutionThread(executionOptions, configStorage: storage);
        executionOptions.PluginDirectories.AddRange(
            GetPluginDirectories(ExecutionThread.GetApplicationDirectory()));
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
#endif
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        pluginsLoader = new Backend.ThriftPlugins.ThriftPluginsLoader(executionThread, executionOptions.PluginDirectories);
#endif
#if ENABLE_PLUGINS && PLUGIN_ASSEMBLY
        pluginsLoader = new Backend.AssemblyPlugins.DotNetAssemblyPluginsLoader(executionThread.FunctionsManager,
            executionOptions.PluginDirectories);
#endif
        PluginsManager pluginsManager = NullPluginsManager.Instance;
#if ENABLE_PLUGINS
        pluginsManager = new DefaultPluginsManager(executionOptions.PluginDirectories, pluginsLoader,
            executionOptions.PluginsRepositoryUri);
        executionThread.PluginsManager = pluginsManager;
#endif
        executionThread.Statistic.CountErrorRows = executionThread.Options.ShowDetailedStatistic;
        new ExecutionThreadBootstrapper().Bootstrap(executionThread, pluginsLoader,
            Backend.Formatters.AdditionalRegistration.Register);
        return new ApplicationRoot(executionThread, pluginsManager, pluginsLoader);
    }

    /// <summary>
    /// Get all plugin directories.
    /// </summary>
    /// <param name="appDirectory">Application local directory.</param>
    /// <returns>Directories.</returns>
    private static IReadOnlyList<string> GetPluginDirectories(string appDirectory)
    {
        var exeDirectory = AppContext.BaseDirectory;
        return new[]
        {
            Path.Combine(appDirectory, ApplicationPluginsDirectory),
            exeDirectory,
            Path.Combine(exeDirectory, ApplicationPluginsDirectory)
        };
    }

    public ApplicationRoot CreateStdoutApplicationRoot(
        ExecutionOptions? executionOptions = null,
        string? columnsSeparator = null,
        Backend.Formatters.TextTableOutput.Style outputStyle = Backend.Formatters.TextTableOutput.Style.Table)
    {
        var root = CreateApplicationRoot(executionOptions);
        var tableOutput = new Backend.Formatters.TextTableOutput(
            stream: StandardInputOutput.GetConsoleOutput(),
            separator: columnsSeparator,
            style: outputStyle);
        root.Thread.Options.DefaultRowsOutput = new Backend.Formatters.PagingOutput(
            tableOutput, pagingRowsCount: Backend.Formatters.PagingOutput.NoLimit, cts: root.Thread.CancellationTokenSource);
        return root;
    }

    /// <summary>
    /// Pre initialization step for logger.
    /// </summary>
    public void InitializeLogger()
    {
        Application.LoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel)
                .AddProvider(new QueryCatConsoleLoggerProvider());
        });
    }
}
