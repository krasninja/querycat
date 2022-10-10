using System.Collections;
using Xunit;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Various SELECT query tests.
/// </summary>
public class SelectTests : BaseTests
{
    [Theory]
    [ClassData(typeof(SelectTestData))]
    public void Select(string fileName)
    {
        // Arrange.
        var data = PrepareTestData(fileName);
        Runner.Run(data.Query);

        // Act.
        var result = GetQueryResult();

        // Assert.
        Assert.Equal(data.Expected, result);
    }

    private sealed class SelectTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var rootDir = GetTestsDirectory();
            foreach (var testFile in Directory.EnumerateFiles(rootDir, "*.yaml"))
            {
                yield return new object[] { Path.GetFileNameWithoutExtension(testFile) };
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
