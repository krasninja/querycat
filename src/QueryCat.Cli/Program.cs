using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Logging;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

/// <summary>
/// Program entry point.
/// </summary>
[Command(Name = "qcat", Description = "The simple data query and transformation utility",
    UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw)]
[VersionOptionFromMember("--version", MemberName = nameof(InfoFunctions.GetVersion))]
[HelpOption("-?|-h|--help")]
internal class Program
{
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
