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
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PluginDebugCommand));

    /// <inheritdoc />
    public PluginDebugCommand() : base("debug", "Setup debug server.")
    {
        var followOption = new Option<bool>("--follow",
            description: "Output appended data as the input source grows.");
        this.AddOption(followOption);

        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var query = OptionsUtils.GetValueForOption(QueryArgument, context);
            var variables = OptionsUtils.GetValueForOption(VariablesOption, context);
            var files = OptionsUtils.GetValueForOption(FilesOption, context);
            var follow = OptionsUtils.GetValueForOption(followOption, context);

            applicationOptions.InitializeLogger();
            var tableOutput = new Backend.Formatters.TextTableOutput(
                stream: Stdio.GetConsoleOutput());
            var options = new AppExecutionOptions
            {
                UseConfig = true,
                RunBootstrapScript = true,
            };
            using var cts = new CancellationTokenSource();

            options.PluginDirectories.AddRange(applicationOptions.PluginDirectories);
            options.DefaultRowsOutput = new PagingOutput(tableOutput, cancellationTokenSource: cts);
            options.FollowTimeout = follow ? QueryOptionsBinder.FollowDefaultTimeout : TimeSpan.Zero;

            await using var thread = new ExecutionThreadBootstrapper(options)
                .WithConfigStorage(new PersistentConfigStorage(
                    Path.Combine(Application.GetApplicationDirectory(), ApplicationOptions.ConfigFileName)))
#if PLUGIN_THRIFT
                .WithPluginsLoader(th => new ThriftPluginsLoader(
                    th,
                    applicationOptions.PluginDirectories,
                    Application.GetApplicationDirectory(),
                    serverPipeName: ThriftPluginClient.TestPipeName,
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
                .WithRegistrations(AdditionalRegistration.Register)
                .WithRegistrations(Backend.Addons.Functions.JsonFunctions.RegisterFunctions)
                .Create();
            await thread.PluginsManager.PluginsLoader.LoadAsync(new PluginsLoadingOptions());
            AddVariables(thread, variables);
            await RunQueryAsync(thread, query, files, cts.Token);
        });
    }
}
#endif
