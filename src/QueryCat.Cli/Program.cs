using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Logging;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

/// <summary>
/// Program entry point.
/// </summary>
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
            case QueryCatException queryCatException:
                Logger.Instance.Error(queryCatException.Message);
                break;
            case Exception exception:
                Logger.Instance.Fatal(exception.Message, exception: exception);
                break;
        }
        Environment.Exit(1);
    }
}
