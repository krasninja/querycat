using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Addons.Formatters;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;
using QueryCat.Backend.PluginsManager;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Main/shared command line options.
/// </summary>
internal sealed class ApplicationOptions
{
    internal const string ConfigFileName = "config.json";
    internal const string ApplicationPluginsDirectory = "plugins";
    internal const string ApplicationPluginsFunctionsCacheDirectory = "func-cache";

    public LogLevel LogLevel { get; init; }

#if ENABLE_PLUGINS
    public string[] PluginDirectories { get; init; } = [];
#endif

    public async Task<ApplicationRoot> CreateApplicationRootAsync(
        AppExecutionOptions? executionOptions = null,
        CancellationToken cancellationToken = default)
    {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        try
        {
            return await CreateApplicationRootInternalAsync(executionOptions, cancellationToken);
        }
        catch (Backend.ThriftPlugins.ProxyNotFoundException)
        {
            await InstallPluginsProxyAsync(askUser: true, cancellationToken: cancellationToken);
            return await CreateApplicationRootInternalAsync(executionOptions, cancellationToken);
        }
#else
        return await CreateApplicationRootInternalAsync(executionOptions, cancellationToken);
#endif
    }

    internal static async Task<bool> InstallPluginsProxyAsync(
        bool askUser = true,
        bool skipIfExists = true,
        CancellationToken cancellationToken = default)
    {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        var applicationDirectory = Application.GetApplicationDirectory(ensureExists: true);
        var pluginsProxyLocalFile = Path.Combine(applicationDirectory,
            Backend.ThriftPlugins.ProxyFile.GetProxyFileName(includeVersion: true));
        if (skipIfExists && File.Exists(pluginsProxyLocalFile))
        {
            return true;
        }

        var key = ConsoleKey.Y;
        if (askUser)
        {
            Console.WriteLine(Resources.Messages.PluginProxyWantToInstall);
            key = Console.ReadKey().Key;
        }
        if (key == ConsoleKey.Y)
        {
            var downloader = new PluginProxyDownloader(Backend.ThriftPlugins.ProxyFile.GetProxyFileName());
            await downloader.DownloadAsync(pluginsProxyLocalFile, cancellationToken);
            Backend.ThriftPlugins.ProxyFile.CleanUpPreviousVersions(applicationDirectory);
            return true;
        }
        return false;
#else
        return false;
#endif
    }

    private async Task<ApplicationRoot> CreateApplicationRootInternalAsync(
        AppExecutionOptions? executionOptions = null,
        CancellationToken cancellationToken = default)
    {
        executionOptions ??= new AppExecutionOptions
        {
            RunBootstrapScript = true,
            UseConfig = true,
        };
#if ENABLE_PLUGINS
        executionOptions.PluginDirectories.AddRange(PluginDirectories);
        executionOptions.PluginDirectories.AddRange(
            GetPluginDirectories(Application.GetApplicationDirectory()));
#endif

        var bootstrapper = new ExecutionThreadBootstrapper(executionOptions)
            .WithConfigStorage(new PersistentConfigStorage(
                Path.Combine(Application.GetApplicationDirectory(), ConfigFileName))
            )
            .WithStandardFunctions()
            .WithRegistrations(Backend.Addons.Functions.JsonFunctions.RegisterFunctions)
            .WithStandardUriResolvers()
            .WithRegistrations(AdditionalRegistration.Register);
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        bootstrapper.WithPluginsLoader(thread => new Backend.ThriftPlugins.ThriftPluginsLoader(
            thread,
            executionOptions.PluginDirectories,
            Application.GetApplicationDirectory(),
            functionsCacheDirectory: Path.Combine(Application.GetApplicationDirectory(),
                ApplicationPluginsFunctionsCacheDirectory),
            minLogLevel: LogLevel,
            maxConnectionsToPlugin: executionOptions.MaxConnectionsToPluginClient)
        );
#endif
#if ENABLE_PLUGINS && PLUGIN_ASSEMBLY
        bootstrapper.WithPluginsLoader(thread =>
            new QueryCat.Backend.AssemblyPlugins.DotNetAssemblyPluginsLoader(
                thread.FunctionsManager,
                executionOptions.PluginDirectories));
#endif
#if ENABLE_PLUGINS
        bootstrapper.WithPluginsManager(pluginsLoader => new DefaultPluginsManager(
            executionOptions.PluginDirectories,
            pluginsLoader,
            platform: Application.GetPlatform(),
            pluginsStorage: new S3PluginsStorage(executionOptions.PluginsRepositoryUri))
        );
#endif
        var thread = bootstrapper.Create();
        await thread.PluginsManager.PluginsLoader.LoadAsync(new PluginsLoadingOptions(), cancellationToken);

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
        return
        [
            Path.Combine(appDirectory, ApplicationPluginsDirectory),
            exeDirectory,
            Path.Combine(exeDirectory, ApplicationPluginsDirectory)
        ];
    }

    public async Task<ApplicationRoot> CreateStdoutApplicationRootAsync(
        AppExecutionOptions? executionOptions = null,
        string? columnsSeparator = null,
        Backend.Formatters.TextTableOutput.Style outputStyle = Backend.Formatters.TextTableOutput.Style.Table1,
        CancellationToken cancellationToken = default)
    {
        var root = await CreateApplicationRootAsync(executionOptions, cancellationToken);
        var tableOutput = new Backend.Formatters.TextTableOutput(
            stream: Stdio.GetConsoleOutput(),
            separator: columnsSeparator,
            style: outputStyle);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        root.Thread.Options.DefaultRowsOutput = new PagingOutput(
            tableOutput, pagingRowsCount: PagingOutput.NoLimit, cancellationTokenSource: cts);
        return root;
    }

    /// <summary>
    /// Pre initialization step for logger.
    /// </summary>
    public void InitializeLogger()
    {
        Application.LoggerFactory = new LoggerFactory(
            providers: [new QueryCatConsoleLoggerProvider()],
            new LoggerFilterOptions
            {
                MinLevel = LogLevel,
            });
    }
}
