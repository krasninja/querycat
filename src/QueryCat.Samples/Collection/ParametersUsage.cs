using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class ParametersUsage : BaseUsage
{
    /// <inheritdoc />
    public override async Task RunAsync()
    {
        using var executionThread = await new ExecutionThreadBootstrapper().CreateAsync();

        await executionThread.RunAsync("hello || ' ' || world", new Dictionary<string, VariantValue>
        {
            ["hello"] = new("Hello"),
            ["world"] = new("World!"),
        });

        Console.WriteLine(executionThread.LastResult.AsString); // Hello World!
    }
}
