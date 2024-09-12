namespace QueryCat.IntegrationTests.DataClasses;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Address? Address { get; init; }

    public List<double> ScoreList { get; init; } = new();

    public Dictionary<string, string> Logins { get; init; } = new();

    public List<Cat> Cats { get; init; } = new();

    public Cat this[int i] => Cats[i];

    public Cat? this[string name] => Cats.Find(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static User GetTestUser1() => new()
    {
        Id = 1,
        Name = "Pavel K.",
        ScoreList = { 5, 4, 5, 3 },
        Address = new()
        {
            Phones = ["+7 933 998 0000", "+7 908 214 0000", "+7 999 445 0000"],
        },
        Cats =
        [
            new Cat("Alisa", 12),
            new Cat("Archi", 10),
        ]
    };

    public static User GetTestUser2() => new()
    {
        Id = 2,
        Name = "John Doe",
        ScoreList = { 1, 2, 3, 4, 5 },
        Address = new(),
        Logins = new Dictionary<string, string>
        {
            ["Google"] = "john.doe@gmail.com",
            ["Yahoo"] = "john.doe@yahoo.com",
            ["Yandex"] = "john.doe@yandex.ru",
        }
    };

    public static User GetTestUser3() => new()
    {
        Id = 1,
        Name = "Aleksandr Drozdov",
        Cats =
        [
            new Cat("Lesya")
        ]
    };
}
