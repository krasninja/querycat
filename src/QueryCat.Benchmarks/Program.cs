using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using QueryCat.Benchmarks.Commands;

namespace QueryCat.Benchmarks;

/// <summary>
/// Entry point class.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            args = ["-h"];
        }

        // Root.
        var rootCommand = new RootCommand
        {
            new CallBenchmarkMethodCommand(),
            new CreateTestCsvFileCommand(),
            new RunBenchmarkCommand(),
        };

        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption("-v", "--version")
            .UseDefaults()
            .Build();
        return await parser.Parse(args).InvokeAsync();
    }
}
