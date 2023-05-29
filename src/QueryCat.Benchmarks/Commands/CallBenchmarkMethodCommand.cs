using System.CommandLine;

namespace QueryCat.Benchmarks.Commands;

internal class CallBenchmarkMethodCommand : Command
{
    public CallBenchmarkMethodCommand() : base("call-benchmark", "Call the specific benchmark method.")
    {
        var methodNameOption = new Option<string>(new[] { "-m", "--method" },
            description: "Benchmark method.")
            {
                IsRequired = true
            };

        this.AddOption(methodNameOption);
        this.SetHandler(methodName =>
        {
            var split = methodName.Split('.');
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
        }, methodNameOption);
    }
}
