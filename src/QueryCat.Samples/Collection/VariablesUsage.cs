using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class VariablesUsage : BaseUsage
{
    /// <inheritdoc />
    public override async Task RunAsync()
    {
        using var executionThread = await new ExecutionThreadBootstrapper().CreateAsync();
        // Define variable in script.
        await executionThread.RunAsync("declare x int := 10;");
        // Define variable in code.
        executionThread.TopScope.Variables["y"] = new VariantValue(5);
        Console.WriteLine((await executionThread.RunAsync("x+y")).AsString); // 15
    }
}
