using System.Globalization;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Tests;

/// <summary>
/// The special runner configuration for tests.
/// </summary>
public class TestThread : ExecutionThread
{
    public const string DataDirectory = "../Data";
    public const string TestDirectory = "../Tests";

    public TestThread()
        : base(new ExecutionOptions
        {
            DefaultRowsOutput = new DsvOutput(new DsvOptions(new MemoryStream())
            {
                HasHeader = false,
                InputOptions = new StreamRowsInputOptions
                {
                    DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
                    {
                        QuoteChars = new[] { '"' },
                        Delimiters = new[] { ',' },
                        BufferSize = 13,
                    },
                    AddInputSourceColumn = false,
                }
            }),
            UseConfig = false,
            AddRowNumberColumn = false,
        })
    {
    }

    /// <summary>
    /// Get test data from file.
    /// </summary>
    /// <param name="fileName">File path.</param>
    /// <returns>Test data.</returns>
    public static TestData GetQueryData(string fileName)
    {
        Directory.SetCurrentDirectory(GetDataDirectory());
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        var testParser = new TestParser(fileName);
        return testParser.Parse();
    }

    /// <summary>
    /// Get last query execution result as string.
    /// </summary>
    /// <returns>Result.</returns>
    public string GetQueryResult()
    {
        var stream = (MemoryStream)((DsvOutput)Options.DefaultRowsOutput).Stream;
        stream.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd().Replace("\r\n", "\n").Trim();
    }

    /// <summary>
    /// Get all test data files for XUnit tests.
    /// </summary>
    /// <returns>Enumerable of data.</returns>
    public static IEnumerable<object[]> GetTestFiles()
    {
        var rootDir = GetTestsDirectory();
        foreach (var testFile in Directory.EnumerateFiles(rootDir, "*.yaml"))
        {
            yield return new object[] { Path.GetFileNameWithoutExtension(testFile) };
        }
    }

    private static string GetDataDirectory()
        => Path.Combine(TestParser.FindTestsDirectory(), DataDirectory);

    private static string GetTestsDirectory()
        => Path.Combine(TestParser.FindTestsDirectory(), TestDirectory);
}
