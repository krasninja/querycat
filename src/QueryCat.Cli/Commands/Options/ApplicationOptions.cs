using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Execution;
using QueryCat.Backend.PluginsManager;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Main/shared command line options.
/// </summary>
internal class ApplicationOptions
{
    internal const string ConfigFileName = "config.json";
    internal const string ApplicationPluginsDirectory = "plugins";
    internal const string ApplicationPluginsFunctionsCacheDirectory = "func-cache";

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
        executionOptions.PluginDirectories.AddRange(
            GetPluginDirectories(ExecutionThread.GetApplicationDirectory()));
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
#endif

        var bootstrapper = new ExecutionThreadBootstrapper(executionOptions)
            .WithConfigStorage(new PersistentInputConfigStorage(
                Path.Combine(ExecutionThread.GetApplicationDirectory(), ConfigFileName))
            )
            .WithStandardFunctions()
            .WithRegistrations(Backend.Formatters.AdditionalRegistration.Register);
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        bootstrapper.WithPluginsLoader(thread => new Backend.ThriftPlugins.ThriftPluginsLoader(
            thread,
            executionOptions.PluginDirectories,
            functionsCacheDirectory: Path.Combine(ExecutionThread.GetApplicationDirectory(),
                ApplicationPluginsFunctionsCacheDirectory))
        );
#endif
#if ENABLE_PLUGINS && PLUGIN_ASSEMBLY
        bootstrapper.WithPluginsLoader(thread =>
            new QueryCat.Backend.AssemblyPlugins.DotNetAssemblyPluginsLoader(thread.FunctionsManager,
            executionOptions.PluginDirectories));
#endif
#if ENABLE_PLUGINS
        bootstrapper.WithPluginsManager(pluginsLoader => new DefaultPluginsManager(
            executionOptions.PluginDirectories,
            pluginsLoader,
            platform: Application.GetPlatform(),
            bucketUri: executionOptions.PluginsRepositoryUri));
#endif
        var thread = bootstrapper.Create();

        return new ApplicationRoot(thread, thread.PluginsManager);
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
        Backend.Formatters.TextTableOutput.Style outputStyle = Backend.Formatters.TextTableOutput.Style.Table1)
    {
        var root = CreateApplicationRoot(executionOptions);
        var tableOutput = new Backend.Formatters.TextTableOutput(
            stream: Stdio.GetConsoleOutput(),
            separator: columnsSeparator,
            style: outputStyle);
        root.Thread.Options.DefaultRowsOutput = new Backend.Formatters.PagingOutput(
            tableOutput, pagingRowsCount: Backend.Formatters.PagingOutput.NoLimit, cts: root.CancellationTokenSource);
        return root;
    }

    /// <summary>
    /// Pre initialization step for logger.
    /// </summary>
    public void InitializeLogger()
    {
        Application.LoggerFactory = new LoggerFactory(
            providers: new[] { new QueryCatConsoleLoggerProvider() },
            new LoggerFilterOptions
            {
                MinLevel = LogLevel,
            });
    }
}
