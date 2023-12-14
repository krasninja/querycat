using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.Samples.Collection;

internal class BasicUsage : BaseUsage
{
    /// <inheritdoc />
    public override void Run()
    {
        var executionThread = new ExecutionThread();
        // Add standard functions.
        new ExecutionThreadBootstrapper().Bootstrap(executionThread);
        var result = VariantValue.Null;

        result = executionThread.Run("1+1");
        Console.WriteLine(result.ToString());

        result = executionThread.Run("2 + 3 * 2");
        Console.WriteLine(result.ToString());

        result = executionThread.Run("sqrt(2) / 3");
        Console.WriteLine(result.ToString("0.00000"));

        result = executionThread.Run("'Hello ' || 'World'");
        Console.WriteLine(result.ToString());

        result = executionThread.Run("date_trunc('day', now())");
        Console.WriteLine(result.ToString());

        result = executionThread.Run("interval '1 day' - interval '1h 1m 1s'");
        Console.WriteLine(result.ToString());

        result = executionThread.Run("lower('AA') = 'aa'");
        Console.WriteLine(result.ToString());
    }
}
