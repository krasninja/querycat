using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Execution;
using QueryCat.Cli.Commands.Options;
#if ENABLE_PLUGINS && PLUGIN_THRIFT
using QueryCat.Backend.ThriftPlugins;
using QueryCat.Plugins.Client;
#endif

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginDebugCommand : BaseQueryCommand
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PluginDebugCommand));

    /// <inheritdoc />
    public PluginDebugCommand() : base("debug", "Setup debug server.")
    {
        this.SetHandler((applicationOptions, query, variables, files) =>
        {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
            applicationOptions.InitializeLogger();
            var tableOutput = new Backend.Formatters.TextTableOutput(
                stream: Backend.IO.Stdio.GetConsoleOutput());
            var options = new ExecutionOptions
            {
                UseConfig = true,
                RunBootstrapScript = true,
            };
            var storage = new PersistentInputConfigStorage(
                Path.Combine(ExecutionThread.GetApplicationDirectory(), ApplicationOptions.ConfigFileName));
            var thread = new ExecutionThread(options, storage);
            var pluginsLoader = new ThriftPluginsLoader(thread, applicationOptions.PluginDirectories,
                serverPipeName: ThriftPluginClient.TestPipeName)
            {
                ForceAuthToken = ThriftPluginClient.TestAuthToken,
                SkipPluginsExecution = true,
            };

            try
            {
                options.PluginDirectories.AddRange(applicationOptions.PluginDirectories);
                options.DefaultRowsOutput = new Backend.Formatters.PagingOutput(tableOutput, cts: thread.CancellationTokenSource);

                new ExecutionThreadBootstrapper().Bootstrap(
                    thread,
                    pluginsLoader,
                    Backend.Formatters.AdditionalRegistration.Register);
                AddVariables(thread, variables);
                RunQuery(thread, query, files);
            }
            finally
            {
                pluginsLoader.Dispose();
                thread.Dispose();
            }

#else
            _logger.LogCritical("Plugins debug is only available for Thrift plugins system.");
#endif
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            VariablesOption,
            FilesOption);
    }
}
#endif
