using McMaster.Extensions.CommandLineUtils;
using QueryCat.Benchmarks.Commands;

namespace QueryCat.Benchmarks;

/// <summary>
/// Entry point class.
/// </summary>
[HelpOption]
[Command("qcat-benchmarks")]
[Subcommand(typeof(CallBenchmarkMethodCommand))]
[Subcommand(typeof(CreateTestCsvFileCommand))]
[Subcommand(typeof(RunBenchmarkCommand))]
public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            args = new[] { "-h" };
        }

        return CommandLineApplication.Execute<Program>(args);
    }
}
