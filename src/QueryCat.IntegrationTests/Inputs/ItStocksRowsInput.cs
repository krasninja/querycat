using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.IntegrationTests.Inputs;

public sealed class ItStocksRowsInput : FetchRowsInput<Stock>
{
    [SafeFunction]
    [FunctionSignature("it_stocks(): object<IRowsInput>")]
    public static VariantValue ItStocks(FunctionCallInfo args)
    {
        return VariantValue.CreateFromObject(new ItStocksRowsInput());
    }

    private readonly List<Stock> _data =
    [
        new Stock("MSFT", "Microsoft Corp", 416.42m),
        new Stock("AAPL", "Apple Inc", 172.62m),
        new Stock("AMZN", "Amazon.com", 174.42m),
        new Stock("NVDA", "NVIDIA Corp", 878.36m),
        new Stock("AMD", "Advanced Micro Devices, Inc.", 191.06m),
        new Stock("INTC", "Intel Corp", 42.64m),
        new Stock("TSLA", "Tesla Inc", 163.57m),
        new Stock("BABA", "Alibaba Group Holdings Ltd ADR", 72.12m),
        new Stock("LNVGY", "Lenovo Group Limited", 24.85m),
    ];

    /// <inheritdoc />
    protected override void Initialize(ClassRowsFrameBuilder<Stock> builder)
    {
        builder
            .AddProperty("id", p => p.Id)
            .AddProperty("name", p => p.CompanyName)
            .AddProperty("usd", p => p.Usd)
            .AddKeyColumn("id", isRequired: true);
    }

    /// <inheritdoc />
    protected override IEnumerable<Stock> GetData(Fetcher<Stock> fetcher)
    {
        var id = GetKeyColumnValue("id");
        var item = _data.First(s => s.Id == id.AsString);
        yield return item;
    }
}
