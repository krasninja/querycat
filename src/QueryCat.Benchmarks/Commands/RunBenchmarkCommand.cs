using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Running;
using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Benchmarks.Commands;

/// <summary>
/// Run all or certain benchmarks.
/// </summary>
[Command("benchmark")]
internal class RunBenchmarkCommand
{
    [Option("-b <benchmark>")]
    [Required]
    public string BenchmarkToRun { get; set; } = string.Empty;

    public void OnExecute()
    {
        var type = typeof(RunBenchmarkCommand).Assembly.GetTypes().FirstOrDefault(t =>
            t.Name.Equals(BenchmarkToRun, StringComparison.OrdinalIgnoreCase));
        if (type == null)
        {
            throw new InvalidOperationException($"Cannot find benchmark '{BenchmarkToRun}'.");
        }
        BenchmarkRunner.Run(type);
    }
}
