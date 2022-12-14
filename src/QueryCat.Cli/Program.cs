using McMaster.Extensions.CommandLineUtils;
using Serilog;
using QueryCat.Backend;
using QueryCat.Backend.Functions.StandardFunctions;

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
                Log.Logger.Error(queryCatException.Message);
                break;
            case Exception exception:
                Log.Logger.Fatal(exception, exception.Message);
                break;
        }
        Environment.Exit(1);
    }
}
