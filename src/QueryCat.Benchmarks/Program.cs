using System.CommandLine;
using QueryCat.Benchmarks.Commands;

namespace QueryCat.Benchmarks;

/// <summary>
/// Entry point class.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Root.
        var rootCommand = new RootCommand
        {
            new CallBenchmarkMethodCommand(),
            new CreateTestCsvFileCommand(),
            new RunBenchmarkCommand(),
        };

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
