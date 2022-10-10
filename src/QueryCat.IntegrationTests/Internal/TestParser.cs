using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QueryCat.IntegrationTests.Internal;

/// <summary>
/// Test parser.
/// </summary>
public class TestParser
{
    private const string TestsDirectory = "Tests";

    private readonly string _fileName;

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public TestParser(string fileName)
    {
        _fileName = fileName;
    }

    public TestData Parse()
    {
        var testsDirectory = FindTestsDirectory();
        var testData = Deserializer.Deserialize<TestData>(
            File.OpenText(Path.Combine(testsDirectory, _fileName + ".yaml")));
        testData.Expected = (testData.Expected ?? string.Empty).Trim();
        testData.Query = testData.Query.Trim();
        return testData;
    }

    public static string FindTestsDirectory()
    {
        var candidates = new[]
        {
            ".",
            "..",
            "../..",
            "../../..",
            "QueryCat.IntegrationTests"
        };

        foreach (var candidate in candidates)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), candidate, TestsDirectory);
            if (Directory.Exists(dir))
            {
                return dir;
            }
        }

        throw new InvalidOperationException("Cannot find tests directory.");
    }
}
