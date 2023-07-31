using Bogus;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;

namespace QueryCat.Plugin.Test;

[FunctionSignature("test_addresses_1")]
internal sealed class AddressRowsInput : FetchInput<Address>
{
    private const int Count = 100;

    /// <inheritdoc />
    protected override void Initialize(ClassRowsFrameBuilder<Address> builder)
    {
        builder
            .AddProperty(p => p.Address1)
            .AddProperty(p => p.Address2)
            .AddProperty(p => p.City)
            .AddProperty(p => p.State)
            .AddProperty(p => p.PostalCode)
            .AddProperty(p => p.Country)
            .AddProperty(p => p.Longitude)
            .AddProperty(p => p.Latitude);
    }

    /// <inheritdoc />
    protected override IEnumerable<Address> GetData(Fetcher<Address> fetcher)
    {
        Randomizer.Seed = new Random(Address.Seed);
        for (var i = 0; i < Count; i++)
        {
            yield return Address.Faker.Generate();
        }
    }
}
