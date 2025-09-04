using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.PluginsManager;
using QueryCat.Cli.Commands.Options;
using QueryCat.Cli.Infrastructure;
#if ENABLE_PLUGINS && PLUGIN_THRIFT
using QueryCat.Backend.ThriftPlugins;
using QueryCat.Plugins.Client;
#endif
#if ENABLE_PLUGINS && PLUGIN_ASSEMBLY
using QueryCat.Backend.AssemblyPlugins;
#endif

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginDebugCommand : BaseQueryCommand
{
    private sealed class NullPluginsStorage : IPluginsStorage
    {
        public static NullPluginsStorage Instance { get; } = new();

        /// <inheritdoc />
        public Task<IReadOnlyList<PluginInfo>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PluginInfo>>([]);

        /// <inheritdoc />
        public Task<Stream> DownloadAsync(string uri, CancellationToken cancellationToken = default)
            => Task.FromResult(Stream.Null);
    }

    /// <inheritdoc />
    public PluginDebugCommand() : base("debug", Resources.Messages.PluginDebugCommand_Description)
    {
        var followOption = new Option<bool>("--follow")
        {
            Description = "Output appended data as the input source grows.",
        };
#if PLUGIN_THRIFT
        var transportOption = new Option<ThriftTransportType>("--transport")
        {
            Description = "Server transport type.",
            DefaultValueFactory = _ => ThriftTransportType.NamedPipes,
        };
#endif
        var registrationTokenOpen = new Option<string>("--token")
        {
            Description = "Registration token for plugin connection.",
        };
        this.Add(followOption);
#if PLUGIN_THRIFT
        this.Add(transportOption);
#endif
        this.Add(registrationTokenOpen);

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var query = parseResult.GetValue(QueryArgument);
            var variables = parseResult.GetValue(VariablesOption);
            var inputs = parseResult.GetValue(InputsOption);
            var files = parseResult.GetValue(FilesOption);
            var follow = parseResult.GetValue(followOption);
#if PLUGIN_THRIFT
            var transport = parseResult.GetValue(transportOption);
#endif
            var token = parseResult.GetValue(registrationTokenOpen);

            applicationOptions.InitializeLogger();
            var logger = Application.LoggerFactory.CreateLogger(nameof(PluginDebugCommand));
            applicationOptions.InitializeAIAssistant();
            var options = new AppExecutionOptions
            {
                UseConfig = true,
                RunBootstrapScript = true,
            };
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            options.PluginDirectories.AddRange(applicationOptions.PluginDirectories);
            options.FollowTimeout = follow ? QueryCommand.FollowDefaultTimeout : TimeSpan.Zero;
            var root = await applicationOptions.CreateApplicationRootAsync(options, cts.Token);
            var thread = root.Thread;
            await AddVariablesAsync(thread, variables, cancellationToken);
            await AddInputsAsync(thread, inputs, cancellationToken);
            await thread.PluginsManager.PluginsLoader.LoadAsync(new PluginsLoadingOptions(), cts.Token);

            logger.LogInformation("Waiting for connections.");
            var debugPluginsManager = new DefaultPluginsManager(
                options.PluginDirectories,
                CreateDebugPluginLoader(
                    thread,
                    applicationOptions,
#if PLUGIN_THRIFT
                    transport,
#endif
                    token),
                NullPluginsStorage.Instance
            );
            await debugPluginsManager.PluginsLoader.LoadAsync(new PluginsLoadingOptions(), cts.Token);
            logger.LogInformation("Press 'q' to exit.");
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    break;
                }
            }
        });
    }

    private PluginsLoader CreateDebugPluginLoader(
        IExecutionThread thread,
        ApplicationOptions applicationOptions,
#if PLUGIN_THRIFT
        ThriftTransportType transport,
#endif
        string? token)
    {
#if PLUGIN_THRIFT
        return new ThriftPluginsLoader(
            thread,
            applicationOptions.PluginDirectories,
            endpoint: CreateEndpoint(transport),
            applicationDirectory: Application.GetApplicationDirectory(),
            debugMode: true,
            minLogLevel: LogLevel.Debug)
        {
            ForceRegistrationToken = string.IsNullOrEmpty(token)
                ? ThriftPluginClient.TestRegistrationToken
                : token,
            SkipPluginsExecution = true,
        };
#elif PLUGIN_ASSEMBLY
        return new DotNetAssemblyPluginsLoader(thread.FunctionsManager, applicationOptions.PluginDirectories);
#endif
    }

#if PLUGIN_THRIFT
    private ThriftEndpoint CreateEndpoint(ThriftTransportType transportType)
    {
        return transportType switch
        {
            ThriftTransportType.NamedPipes => ThriftEndpoint.CreateNamedPipe(ThriftPluginClient.TestPipeName),
            ThriftTransportType.Tcp => ThriftEndpoint.CreateTcp(ThriftPluginClient.DefaultTcpPort),
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
        };
    }
#endif
}
#endif
