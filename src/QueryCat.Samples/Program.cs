using QueryCat.Samples.Collection;

namespace QueryCat.Samples;

internal class Program
{
    private static readonly BaseUsage[] _samples =
    {
        new BasicUsage(),
        new CustomFunctionUsage(),
        new VariablesUsage(),
    };

    internal static void Main(string[] args)
    {
        foreach (var sample in _samples)
        {
            sample.Run();
        }
    }
}
