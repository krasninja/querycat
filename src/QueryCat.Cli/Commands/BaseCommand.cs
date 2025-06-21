using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal abstract class BaseCommand : Command
{
    private static Option<LogLevel> LogLevelOption { get; } = new("--log-level")
    {
        Description = Resources.Messages.QueryCommand_LogLevelDescription,
        DefaultValueFactory = _ =>
        {
#if DEBUG
            return LogLevel.Debug;
#else
            return LogLevel.Information;
#endif
        }
    };

    private static Option<string[]> PluginDirectoriesOption { get; } = new("--plugin-dirs")
    {
        Description = Resources.Messages.QueryCommand_PluginDirectoriesCommand,
        AllowMultipleArgumentsPerToken = true,
#if !ENABLE_PLUGINS
        Hidden = true,
#endif
    };

    /// <inheritdoc />
    protected BaseCommand(string name, string? description = null) : base(name, description)
    {
        Add(LogLevelOption);
#if ENABLE_PLUGINS
        Add(PluginDirectoriesOption);
#endif
    }

    protected static ApplicationOptions GetApplicationOptions(ParseResult parseResult)
    {
        return new ApplicationOptions
        {
            LogLevel = parseResult.GetValue(LogLevelOption),
#if ENABLE_PLUGINS
            PluginDirectories = (parseResult.GetValue(PluginDirectoriesOption) ?? [])
                .SelectMany(d => d.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                .ToArray(),
#endif
        };
    }
}
