using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class CustomInlineFunctionUsage : BaseUsage
{
    /// <inheritdoc />
    public override void Run()
    {
        var executionThread = new ExecutionThreadBootstrapper().Create();
        executionThread.FunctionsManager.RegisterFunction("secret(a: string, b: numeric): string", thread =>
        {
            var a = thread.Stack[0];
            var b = thread.Stack[1];
            return new VariantValue(a + b.ToString());
        });

        Console.WriteLine(executionThread.Run("secret('num:', 10.25::numeric)").ToString()); // num:10.25
    }
}
