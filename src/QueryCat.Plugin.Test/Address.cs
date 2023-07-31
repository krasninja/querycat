using Bogus;

namespace QueryCat.Plugin.Test;

/// <summary>
/// Test address class.
/// </summary>
internal sealed class Address
{
    internal const int Seed = 5625470;

    public string Address1 { get; set; } = string.Empty;

    public string Address2 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public double Longitude { get; set; }

    public double Latitude { get; set; }

    internal static readonly Faker<Address> Faker = new Faker<Address>()
        .RuleFor(u => u.Address1, (f, a) => f.Address.StreetAddress(useFullAddress: true))
        .RuleFor(u => u.Address2, (f, a) => f.Address.SecondaryAddress())
        .RuleFor(u => u.City, (f, a) => f.Address.City())
        .RuleFor(u => u.State, (f, a) => f.Address.StateAbbr())
        .RuleFor(u => u.PostalCode, (f, a) => f.Address.ZipCode())
        .RuleFor(u => u.Country, (f, a) => f.Address.Country())
        .RuleFor(u => u.Longitude, (f, a) => f.Address.Longitude())
        .RuleFor(u => u.Latitude, (f, a) => f.Address.Latitude());
}
