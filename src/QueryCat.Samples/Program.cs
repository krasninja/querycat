using QueryCat.Samples.Collection;

namespace QueryCat.Samples;

internal class Program
{
    private static readonly BaseUsage[] _samples =
    {
        new AsyncFunctionUsage(),
        new BasicUsage(),
        new CollectionsUsage(),
        new CustomFunctionUsage(),
        new CustomInlineFunctionUsage(),
        new ObjectExpressionsUsage(),
        new ParametersUsage(),
        new VariablesUsage(),
    };

    internal static void Main(string[] args)
    {
        var pattern = args.Length > 0 ? args[0] : string.Empty;

        foreach (var sample in _samples)
        {
            var name = sample.GetType().Name;
            if (!string.IsNullOrEmpty(pattern) && !name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            Console.WriteLine(name);
            Console.WriteLine(new string('=', 50));
            sample.Run();
        }
    }
}
