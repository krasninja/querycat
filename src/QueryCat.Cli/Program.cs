using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Logging;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

/// <summary>
/// Program entry point.
/// </summary>
[Command(Name = "qcat", Description = "The simple data query and transformation utility")]
[VersionOptionFromMember("--version", MemberName = nameof(InfoFunctions.GetVersion))]
[HelpOption("-?|-h|--help")]
internal class Program
{
    [Argument(0, Description = "SQL-like query")]
    [Required]
    private string Query { get; } = string.Empty;

    [Option("--stat", Description = "Show statistic")]
    private bool ShowStatistic { get; }

    [Option("--ast", Description = "Show AST")]
    private bool ShowAst { get; } = false;

    [Option("--row-number", Description = "Include row number column")]
    private bool RowNumber { get; } = false;

    [Option("--log-level", Description = "Set log level")]
    private LogLevel LogLevel { get; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Info;
#endif

    [Option("--plugin-dir", Description = "Plugin directory")]
    private string[] PluginDirectories { get; } = Array.Empty<string>();

    /// <summary>
    /// Entry point.
    /// </summary>
    /// <param name="args">Application execution arguments.</param>
    /// <returns>Error code.</returns>
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        Logger.Instance.AddHandlers(new ConsoleLogHandler());

        if (args.Length < 1)
        {
            args = new[] { "-h" };
        }
        // Fast execution without arguments processing.
        if (args.Length == 1 && args[0].StartsWith('"'))
        {
            new QueryCommand().OnExecuteInternal(PhysicalConsole.Singleton);
            return 0;
        }

        return CommandLineApplication.Execute<QueryCommand>(args);
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        switch (e.ExceptionObject)
        {
            case Exception exception:
                Logger.Instance.Fatal(exception.Message);
                break;
        }
        Environment.Exit(1);
    }
}
