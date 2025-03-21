using System.Globalization;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class CustomInlineFunctionUsage : BaseUsage
{
    /// <inheritdoc />
    public override async Task RunAsync()
    {
        await using var executionThread = new ExecutionThreadBootstrapper().Create();
        var function = executionThread.FunctionsManager.Factory.CreateFromSignature("secret(a: string, b: numeric): string", (IExecutionThread thread) =>
        {
            var a = thread.Stack[0];
            var b = thread.Stack[1];
            return new VariantValue(a + b.ToString(CultureInfo.InvariantCulture));
        });
        executionThread.FunctionsManager.RegisterFunction(function);

        var result = await executionThread.RunAsync("secret('num:', 10.25::numeric)");
        Console.WriteLine(result.ToString(CultureInfo.InvariantCulture)); // num:10.25
    }
}
