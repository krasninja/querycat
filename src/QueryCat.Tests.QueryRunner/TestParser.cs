using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QueryCat.Tests.QueryRunner;

/// <summary>
/// Test parser.
/// </summary>
public sealed class TestParser
{
    private const string TestsDirectory = "Tests";

    private readonly string _fileName;

    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public TestParser(string fileName)
    {
        _fileName = fileName;
    }

    public TestData Parse()
    {
        var testsDirectory = FindTestsDirectory();
        var testData = _deserializer.Deserialize<TestData>(
            File.OpenText(Path.Combine(testsDirectory, AddYamlExtension(_fileName)))
        );
        testData.Expected = (testData.Expected ?? string.Empty).Trim();
        testData.Query = testData.Query.Replace("\r\n", "\n").Trim();
        return testData;
    }

    private static string AddYamlExtension(string fileName)
        => !fileName.EndsWith(".yaml") ? fileName + ".yaml" : fileName;

    public static string FindTestsDirectory()
    {
        var candidates = new[]
        {
            ".",
            "..",
            "../..",
            "../../..",
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
