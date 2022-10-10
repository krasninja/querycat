using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Benchmarks.Commands;

[Command("call-benchmark")]
internal class CallBenchmarkMethodCommand
{
    [Option("-b <benchmark>.<method>")]
    [Required]
    public string BenchmarkToRun { get; set; } = string.Empty;

    public void OnExecute()
    {
        var split = BenchmarkToRun.Split('.');
        if (split.Length != 2)
        {
            throw new InvalidOperationException("Incorrect benchmark format.");
        }

        var type = typeof(RunBenchmarkCommand).Assembly.GetTypes().FirstOrDefault(t =>
            t.Name.Equals(split[0], StringComparison.OrdinalIgnoreCase));
        if (type == null)
        {
            throw new InvalidOperationException($"Cannot find benchmark '{split[0]}'.");
        }
        var instance = Activator.CreateInstance(type);
        var method = type.GetMethod(split[1]);
        if (method == null)
        {
            throw new InvalidOperationException("Cannot find method.");
        }

        method.Invoke(instance, Array.Empty<object>());
    }
}
