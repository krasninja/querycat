using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Addons.Formatters;
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
    public string[] PluginDirectories { get; init; } = [];
#endif

    public ApplicationRoot CreateApplicationRoot(AppExecutionOptions? executionOptions = null)
    {
        executionOptions ??= new AppExecutionOptions
        {
            RunBootstrapScript = true,
            UseConfig = true,
        };
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(
            GetPluginDirectories(ExecutionThread.GetApplicationDirectory()));
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
#endif

        var bootstrapper = new ExecutionThreadBootstrapper(executionOptions)
            .WithConfigStorage(new PersistentInputConfigStorage(
                Path.Combine(ExecutionThread.GetApplicationDirectory(), ConfigFileName))
            )
            .WithStandardFunctions()
            .WithRegistrations(Backend.Addons.Functions.JsonFunctions.RegisterFunctions)
            .WithStandardUriResolvers()
            .WithRegistrations(AdditionalRegistration.Register);
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        bootstrapper.WithPluginsLoader(thread => new Backend.ThriftPlugins.ThriftPluginsLoader(
            thread,
            executionOptions.PluginDirectories,
            ExecutionThread.GetApplicationDirectory(),
            functionsCacheDirectory: Path.Combine(ExecutionThread.GetApplicationDirectory(),
                ApplicationPluginsFunctionsCacheDirectory),
            minLogLevel: LogLevel)
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
        AppExecutionOptions? executionOptions = null,
        string? columnsSeparator = null,
        Backend.Formatters.TextTableOutput.Style outputStyle = Backend.Formatters.TextTableOutput.Style.Table1)
    {
        var root = CreateApplicationRoot(executionOptions);
        var tableOutput = new Backend.Formatters.TextTableOutput(
            stream: Stdio.GetConsoleOutput(),
            separator: columnsSeparator,
            style: outputStyle);
        root.Thread.Options.DefaultRowsOutput = new PagingOutput(
            tableOutput, pagingRowsCount: PagingOutput.NoLimit, cancellationTokenSource: root.CancellationTokenSource);
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
