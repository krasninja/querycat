using Bogus;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Sample.Models;

namespace QueryCat.Plugins.Sample.Inputs;

internal sealed class AddressIterator : IRowsIterator
{
    [SafeFunction]
    [FunctionSignature("sample_get_addresses_2(): object<IRowsIterator>")]
    public static VariantValue AddressIteratorFunction(IExecutionThread thread)
    {
        return VariantValue.CreateFromObject(new AddressIterator());
    }

    private const int Count = 100;

    private int _currentIndex = 0;

    /// <inheritdoc />
    public Column[] Columns { get; } =
    {
        new("address1", DataType.String),
        new("address2", DataType.String),
        new("city", DataType.String),
        new("state", DataType.String),
        new("zip", DataType.String),
        new("country", DataType.String),
        new("longitude", DataType.Float),
        new("latitude", DataType.Float),
    };

    /// <inheritdoc />
    public Row Current { get; }

    public AddressIterator()
    {
        Randomizer.Seed = new Random(Address.Seed);
        Current = new Row(Columns);
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_currentIndex >= Count)
        {
            return ValueTask.FromResult(false);
        }

        var address = Address.Faker.Generate();
        Current["address1"] = new VariantValue(address.Address1);
        Current["address2"] = new VariantValue(address.Address2);
        Current["city"] = new VariantValue(address.City);
        Current["state"] = new VariantValue(address.State);
        Current["zip"] = new VariantValue(address.PostalCode);
        Current["country"] = new VariantValue(address.Country);
        Current["longitude"] = new VariantValue(address.Longitude);
        Current["latitude"] = new VariantValue(address.Latitude);

        _currentIndex++;
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _currentIndex = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
    }
}
