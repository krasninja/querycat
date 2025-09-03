using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Addons.Formatters;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;
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
internal class PluginDebugCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public PluginDebugCommand() : base("debug", Resources.Messages.PluginDebugCommand_Description)
    {
        var followOption = new Option<bool>("--follow")
        {
            Description = "Output appended data as the input source grows.",
        };
        var transportOption = new Option<ThriftTransportType>("--transport")
        {
            Description = "Server transport type.",
            DefaultValueFactory = _ => ThriftTransportType.NamedPipes,
        };
        this.Add(followOption);
        this.Add(transportOption);

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var query = parseResult.GetValue(QueryArgument);
            var variables = parseResult.GetValue(VariablesOption);
            var inputs = parseResult.GetValue(InputsOption);
            var files = parseResult.GetValue(FilesOption);
            var follow = parseResult.GetValue(followOption);
            var transport = parseResult.GetValue(transportOption);

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

            await using var thread = new ExecutionThreadBootstrapper(options)
                .WithConfigStorage(new PersistentConfigStorage(
                    Path.Combine(Application.GetApplicationDirectory(), ApplicationOptions.ConfigFileName)))
#if PLUGIN_THRIFT
                .WithPluginsLoader(th => new ThriftPluginsLoader(
                    th,
                    applicationOptions.PluginDirectories,
                    endpoint: CreateEndpoint(transport),
                    applicationDirectory: Application.GetApplicationDirectory(),
                    debugMode: true,
                    minLogLevel: LogLevel.Debug)
                {
                    ForceRegistrationToken = ThriftPluginClient.TestRegistrationToken,
                    SkipPluginsExecution = true,
                })
#elif PLUGIN_ASSEMBLY
                .WithPluginsLoader(th => new DotNetAssemblyPluginsLoader(
                    th.FunctionsManager,
                    applicationOptions.PluginDirectories))
#endif
                .WithStandardFunctions()
                .WithRegistrations(AdditionalRegistration.Register)
                .WithRegistrations(Backend.Addons.Functions.JsonFunctions.RegisterFunctions)
                .Create();
            await AddVariablesAsync(thread, variables, cancellationToken);
            await AddInputsAsync(thread, inputs, cancellationToken);
            logger.LogInformation("Waiting for connections.");
            await thread.PluginsManager.PluginsLoader.LoadAsync(new PluginsLoadingOptions(), cts.Token);
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
