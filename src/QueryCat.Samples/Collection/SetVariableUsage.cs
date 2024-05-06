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
    public override void Run()
    {
        using var executionThread = new ExecutionThreadBootstrapper().Create();
        var data = new Dictionary<int, Product>
        {
            [1] = new(10m),
            [2] = new(25.53m),
        };

        executionThread.Run("SET data[1].Price := newval;",
            new Dictionary<string, VariantValue>
            {
                ["data"] = VariantValue.CreateFromObject(data),
                ["newval"] = new(12.34m),
            });
        Console.WriteLine(data[1].Price); // 12.34
    }
}
