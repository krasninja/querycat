using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Addons.Formatters;
using QueryCat.Backend.Core;
using QueryCat.Backend.Execution;
using QueryCat.Cli.Commands.Options;
using QueryCat.Cli.Infrastructure;
#if ENABLE_PLUGINS
#if PLUGIN_THRIFT
using QueryCat.Backend.ThriftPlugins;
using QueryCat.Plugins.Client;
#elif PLUGIN_ASSEMBLY
using QueryCat.Backend.AssemblyPlugins;
#endif
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

        this.SetHandler((applicationOptions, query, variables, files, follow) =>
        {
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

            using var thread = new ExecutionThreadBootstrapper(options)
                .WithConfigStorage(new PersistentInputConfigStorage(
                    Path.Combine(ExecutionThread.GetApplicationDirectory(), ApplicationOptions.ConfigFileName)))
#if PLUGIN_THRIFT
                .WithPluginsLoader(th => new ThriftPluginsLoader(
                    th,
                    applicationOptions.PluginDirectories,
                    serverPipeName: ThriftPluginClient.TestPipeName,
                    debugMode: true,
                    minLogLevel: LogLevel.Debug)
                {
                    ForceAuthToken = ThriftPluginClient.TestAuthToken,
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
            AddVariables(thread, variables);
            RunQuery(thread, query, files, cts.Token);
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            VariablesOption,
            FilesOption,
            followOption);
    }
}
#endif
