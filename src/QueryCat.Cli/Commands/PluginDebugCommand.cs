using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Providers;
using QueryCat.Cli.Commands.Options;
#if ENABLE_PLUGINS && PLUGIN_THRIFT
using QueryCat.Backend.ThriftPlugins;
using QueryCat.Cli.Commands.Options;
#endif

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginDebugCommand : BaseQueryCommand
{
    private const string PipeName = "qcat-test";
    private const string AuthToken = "test";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<PluginDebugCommand>();

    /// <inheritdoc />
    public PluginDebugCommand() : base("debug", "Setup debug server.")
    {
        this.SetHandler((applicationOptions, query, variables, files) =>
        {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
            applicationOptions.InitializeLogger();
            var tableOutput = new TextTableOutput(
                stream: StandardInputOutput.GetConsoleOutput());
            var options = new ExecutionOptions
            {
                UseConfig = true,
                RunBootstrapScript = true,
            };
            var storage = new PersistentInputConfigStorage(
                Path.Combine(ExecutionThread.GetApplicationDirectory(), ApplicationOptions.ConfigFileName));
            var thread = new ExecutionThread(options, storage);
            options.PluginDirectories.AddRange(applicationOptions.PluginDirectories);
            options.DefaultRowsOutput = new PagingOutput(tableOutput, cts: thread.CancellationTokenSource);
            var pluginsLoader = new ThriftPluginsLoader(thread, applicationOptions.PluginDirectories, PipeName)
            {
                ForceAuthToken = AuthToken,
                SkipPluginsExecution = true,
            };
            new ExecutionThreadBootstrapper().Bootstrap(thread, pluginsLoader);
            AddVariables(thread, variables);
            RunQuery(thread, query, files);
            thread.Dispose();
            pluginsLoader.Dispose();
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
