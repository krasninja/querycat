using System.Globalization;
using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class BasicUsage : BaseUsage
{
    /// <inheritdoc />
    public override async Task RunAsync()
    {
        // Use "ExecutionThreadBootstrapper" class to create execution thread. It allows
        // run queries.
        using var executionThread = await new ExecutionThreadBootstrapper()
            .WithStandardFunctions() // Add standard functions (math, string, JSON and others).
            .WithStandardUriResolvers()
            .CreateAsync();
        var result = VariantValue.Null;

        result = await executionThread.RunAsync("1+1");
        Console.WriteLine(result.AsString); // 2

        result = await executionThread.RunAsync("2 + 3 * 2");
        Console.WriteLine(result.AsString); // 8

        result = await executionThread.RunAsync("sqrt(2) / 3");
        Console.WriteLine(result.ToString("0.00000")); // 0.47140

        result = await executionThread.RunAsync("'Hello ' || 'World'");
        Console.WriteLine(result.ToString(CultureInfo.InvariantCulture)); // Hello World

        result = await executionThread.RunAsync("date_trunc('day', now())");
        Console.WriteLine(result.ToString(CultureInfo.InvariantCulture)); // 05/06/2024 00:00:00

        result = await executionThread.RunAsync("interval '1 day' - interval '1h 1m 1s'");
        Console.WriteLine(result.ToString(CultureInfo.InvariantCulture)); // 22:58:59

        result = await executionThread.RunAsync("lower('AA') = 'aa'");
        Console.WriteLine(result.ToString(CultureInfo.InvariantCulture)); // True
    }
}
