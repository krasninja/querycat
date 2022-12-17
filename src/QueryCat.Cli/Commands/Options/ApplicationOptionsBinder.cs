using System.CommandLine;
using System.CommandLine.Binding;
using Serilog.Events;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Command line parameters binder for <see cref="ApplicationOptions" />.
/// </summary>
internal class ApplicationOptionsBinder : BinderBase<ApplicationOptions>
{
    private readonly Option<LogEventLevel> _logLevelOption;

#if ENABLE_PLUGINS
    private readonly Option<string[]> _pluginDirectoriesOption;
#endif

    /// <inheritdoc />
    public ApplicationOptionsBinder(
        Option<LogEventLevel> logLevelOption,
        Option<string[]> pluginDirectoriesOption)
    {
        _logLevelOption = logLevelOption;
#if ENABLE_PLUGINS
        _pluginDirectoriesOption = pluginDirectoriesOption;
#endif
    }

    /// <inheritdoc />
    protected override ApplicationOptions GetBoundValue(BindingContext bindingContext)
    {
        return new ApplicationOptions
        {
            LogLevel = bindingContext.ParseResult.GetValueForOption(_logLevelOption),
#if ENABLE_PLUGINS
            PluginDirectories = bindingContext.ParseResult.GetValueForOption(_pluginDirectoriesOption) ?? Array.Empty<string>(),
#endif
        };
    }
}
