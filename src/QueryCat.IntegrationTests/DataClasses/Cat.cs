namespace QueryCat.IntegrationTests.DataClasses;

public class Cat
{
    public string Name { get; }

    public int? Age { get; set; }

    public Cat(string name, int? age = null)
    {
        Name = name;
        Age = age;
    }
}
