using QueryCat.Backend.Core.Fetch;

namespace QueryCat.IntegrationTests.Inputs;

public sealed class KeyedRowsInput : FetchRowsInput<Stock>
{
    private readonly List<Stock> _data = new()
    {
        new Stock("MSFT", "Microsoft", 40),
    };

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
