namespace QueryCat.IntegrationTests.DataClasses;

public class Address
{
    public string City { get; init; } = string.Empty;

    public List<string> Phones { get; set; } = new();
}
