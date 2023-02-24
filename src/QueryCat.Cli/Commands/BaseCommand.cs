using System.CommandLine;
using Serilog.Events;

namespace QueryCat.Cli.Commands;

internal abstract class BaseCommand : Command
{
    public static Option<LogEventLevel> LogLevelOption { get; } = new("--log-level",
        description: "Log level.",
        getDefaultValue: () =>
        {
#if DEBUG
            return LogEventLevel.Debug;
#else
            return LogEventLevel.Information;
#endif
        });

    public static Option<string[]> PluginDirectoriesOption { get; } = new("--plugin-dirs",
        description: "Plugin directories.")
    {
        AllowMultipleArgumentsPerToken = true,
#if !ENABLE_PLUGINS
        IsHidden = true,
#endif
    };

    /// <inheritdoc />
    public BaseCommand(string name, string? description = null) : base(name, description)
    {
        Add(LogLevelOption);
#if ENABLE_PLUGINS
        Add(PluginDirectoriesOption);
#endif
    }
}
