using System.CommandLine;
using BenchmarkDotNet.Running;

namespace QueryCat.Benchmarks.Commands;

/// <summary>
/// Run all or certain benchmarks.
/// </summary>
internal class RunBenchmarkCommand : Command
{
    public RunBenchmarkCommand() : base("benchmark")
    {
        var benchmarkOption = new Option<string>("-b",
            description: "Benchmark to run")
            {
                IsRequired = true,
            };

        this.AddOption(benchmarkOption);
        this.SetHandler(benchmark =>
        {
            var type = typeof(RunBenchmarkCommand).Assembly.GetTypes().FirstOrDefault(t =>
                t.Name.Equals(benchmark, StringComparison.OrdinalIgnoreCase));
            if (type == null)
            {
                throw new InvalidOperationException($"Cannot find benchmark '{benchmark}'.");
            }
            BenchmarkRunner.Run(type);
        }, benchmarkOption);
    }
}
