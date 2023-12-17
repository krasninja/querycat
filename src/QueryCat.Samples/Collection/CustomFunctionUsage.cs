using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.Samples.Collection;

internal class CustomFunctionUsage : BaseUsage
{
    /// <inheritdoc />
    public override void Run()
    {
        var executionThread = new ExecutionThread();
        executionThread.FunctionsManager.RegisterFunction("secret(a: string, b: numeric): string", args =>
        {
            var a = args.GetAt(0);
            var b = args.GetAt(1);
            return new VariantValue(a + b.ToString());
        });

        Console.WriteLine(executionThread.Run("secret('num:', 10.25::numeric)").ToString());
    }
}