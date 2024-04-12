namespace QueryCat.IntegrationTests.DataClasses;

public class User
{
    public int Id { get; set; }

    public string Name { get; init; } = string.Empty;

    public Address? Address { get; init; }

    public static User GetTestUser1() => new()
    {
        Id = 1,
        Name = "Pavel K.",
        Address = new()
        {
            Phones = ["+7 933 998 0000", "+7 908 214 0000", "+7 999 445 0000"],
        }
    };
}
