using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class ParametersUsage : BaseUsage
{
    /// <inheritdoc />
    public override void Run()
    {
        using var executionThread = new ExecutionThreadBootstrapper()
            .Create();

        executionThread.Run("hello || ' ' || world", new Dictionary<string, VariantValue>
        {
            ["hello"] = new("Hello"),
            ["world"] = new("World!"),
        });

        Console.WriteLine(executionThread.LastResult.AsString);
    }
}
