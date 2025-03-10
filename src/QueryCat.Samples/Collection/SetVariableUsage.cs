using QueryCat.Backend;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class SetVariableUsage : BaseUsage
{
    private class Product
    {
        public decimal Price { get; set; }

        public Product(decimal price)
        {
            Price = price;
        }
    }

    /// <inheritdoc />
    public override async Task RunAsync()
    {
        await using var executionThread = new ExecutionThreadBootstrapper().Create();
        var data = new Dictionary<int, Product>
        {
            [1] = new(10m),
            [2] = new(25.53m),
        };

        executionThread.TopScope.Variables["data"] = VariantValue.CreateFromObject(data);
        await executionThread.RunAsync("SET data[1].Price := newval;",
            new Dictionary<string, VariantValue>
            {
                ["newval"] = new(12.34m),
            });
        Console.WriteLine(data[1].Price); // 12.34

        await executionThread.RunAsync(
            "SET data[2].Price := newval;",
            new Dictionary<string, VariantValue>
            {
                ["newval"] = new("43.21"),
            });
        Console.WriteLine(data[2].Price); // 43.21, implicit conversation.
    }
}
