using System.Text;
using QueryCat.Backend;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Samples.Collection;

internal class CollectionsUsage : BaseUsage
{
    private class Building(string name, string postalCode)
    {
        public string Name { get; } = name;

        public string PostalCode { get; } = postalCode;
    }

    private class City(string postalCode, string city)
    {
        public string PostalCode { get; } = postalCode;

        public string CityName { get; } = city;
    }

    /// <inheritdoc />
    public override async Task RunAsync()
    {
        // Arrange.
        var executionThread = new ExecutionThreadBootstrapper().Create();
        var buildings = new List<Building>
        {
            new("Hotel Indigo San Diego-Gaslamp Quarter, an IHG Hotel", "92101"),
            new("San Diego Chinese Historical Museum Chuang Archive and Learning Center", "92101"),
            new("Aristocrat Gallery", "92101"),
            new("Gus's World Famous Fried Chicken", "92701"),
            new("Music Center Garage", "90012"),
            new("Homeboy Industries", "90012"),
            new("Saritasa - Custom Software Development Company", "20411"),
            new("Bloomberg", "10022"),
        };
        var cities = new List<City>
        {
            new("92101", "San Diego, CA"),
            new("92701", "Santa Ana, CA"),
            new("90012", "Los Angeles, CA"),
            new("20411", "Newport Beach, CA"),
        };

        var buildingsInput = EnumerableRowsInput<Building>.FromSource(buildings,
            builder => builder
                .AddProperty("name", f => f.Name)
                .AddProperty("zip", f => f.PostalCode));
        var citiesInput = EnumerableRowsInput<City>.FromSource(cities,
            builder => builder
                .AddProperty("zip", f => f.PostalCode)
                .AddProperty("city", f => f.CityName));

        // Act.
        executionThread.TopScope.Variables["buildings"] = VariantValue.CreateFromObject(buildingsInput);
        executionThread.TopScope.Variables["cities"] = VariantValue.CreateFromObject(citiesInput);
        var result = await executionThread.RunAsync(
            "SELECT b.name, c.city FROM buildings b LEFT JOIN cities c ON b.zip = c.zip;");

        // Out.
        var sb = new StringBuilder();
        await new TextTableOutput(sb).WriteAsync(result.AsRequired<IRowsIterator>(), adjustColumnsLengths: true);
        Console.WriteLine(sb);
        /*
         | b.name                                                                 | c.city            |
         | ---------------------------------------------------------------------- | ----------------- |
         | Hotel Indigo San Diego-Gaslamp Quarter, an IHG Hotel                   | San Diego, CA     |
         | San Diego Chinese Historical Museum Chuang Archive and Learning Center | San Diego, CA     |
         ...
         */
    }
}
