using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class BasicUsage : BaseUsage
{
    /// <inheritdoc />
    public override void Run()
    {
        // Use "ExecutionThreadBootstrapper" class to create execution thread. It allows
        // run queries.
        using var executionThread = new ExecutionThreadBootstrapper()
            .WithStandardFunctions() // Add standard functions (math, string, JSON and others).
            .WithStandardUriResolvers()
            .Create();
        var result = VariantValue.Null;

        result = executionThread.Run("1+1");
        Console.WriteLine(result.AsString); // 2

        result = executionThread.Run("2 + 3 * 2");
        Console.WriteLine(result.AsString); // 8

        result = executionThread.Run("sqrt(2) / 3");
        Console.WriteLine(result.ToString("0.00000")); // 0.47140

        result = executionThread.Run("'Hello ' || 'World'");
        Console.WriteLine(result.ToString()); // Hello World

        result = executionThread.Run("date_trunc('day', now())");
        Console.WriteLine(result.ToString()); // 05/06/2024 00:00:00

        result = executionThread.Run("interval '1 day' - interval '1h 1m 1s'");
        Console.WriteLine(result.ToString()); // 22:58:59

        result = executionThread.Run("lower('AA') = 'aa'");
        Console.WriteLine(result.ToString()); // True
    }
}
