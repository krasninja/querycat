using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Providers;
using QueryCat.Cli.Commands.Options;
#if ENABLE_PLUGINS && PLUGIN_THRIFT
using QueryCat.Backend.ThriftPlugins;
#endif

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginDebugCommand : BaseQueryCommand
{
    private const string PipeName = "qcat-test";
    private const string AuthToken = "test";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PluginDebugCommand));

    /// <inheritdoc />
    public PluginDebugCommand() : base("debug", "Setup debug server.")
    {
        this.SetHandler((applicationOptions, query, variables, files) =>
        {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
            applicationOptions.InitializeLogger();
            var tableOutput = new Backend.Formatters.TextTableOutput(
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
            options.DefaultRowsOutput = new Backend.Formatters.PagingOutput(tableOutput, cts: thread.CancellationTokenSource);
            var pluginsLoader = new ThriftPluginsLoader(thread, applicationOptions.PluginDirectories, serverPipeName: PipeName)
            {
                ForceAuthToken = AuthToken,
                SkipPluginsExecution = true,
            };
            new ExecutionThreadBootstrapper().Bootstrap(
                thread,
                pluginsLoader,
                Backend.Formatters.AdditionalRegistration.Register);
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
