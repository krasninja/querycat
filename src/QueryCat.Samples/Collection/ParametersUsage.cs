using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class ParametersUsage : BaseUsage
{
    /// <inheritdoc />
    public override async Task RunAsync()
    {
        await using var executionThread = new ExecutionThreadBootstrapper().Create();

        var result = await executionThread.RunAsync("hello || ' ' || world", new Dictionary<string, VariantValue>
        {
            ["hello"] = new("Hello"),
            ["world"] = new("World!"),
        });

        Console.WriteLine(result.AsString); // Hello World!
    }
}
