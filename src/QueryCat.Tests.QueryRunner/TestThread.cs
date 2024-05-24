using System.Globalization;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Tests.QueryRunner;

/// <summary>
/// The special runner configuration for tests.
/// </summary>
public static class TestThread
{
    public const string DataDirectory = "../Data";
    public const string TestDirectory = "../Tests";

    public static ExecutionThreadBootstrapper CreateBootstrapper()
    {
        return new ExecutionThreadBootstrapper(new ExecutionOptions
            {
                DefaultRowsOutput = CreateDsvOutput(),
                UseConfig = false,
                AddRowNumberColumn = false,
            })
            .WithAstCache()
            .WithStandardFunctions()
            .WithStandardUriResolvers();
    }

    private static DsvOutput CreateDsvOutput()
    {
        return new DsvOutput(
            new DsvOptions(new MemoryStream())
            {
                HasHeader = false,
                InputOptions = new StreamRowsInputOptions
                {
                    DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
                    {
                        QuoteChars = ['"'],
                        Delimiters = [','],
                        BufferSize = 13,
                        Culture = Application.Culture,
                    },
                    AddInputSourceColumn = false,
                }
            });
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
        Application.Culture = CultureInfo.InvariantCulture;
        var testParser = new TestParser(fileName);
        return testParser.Parse();
    }

    /// <summary>
    /// Get last query execution result as string.
    /// </summary>
    /// <param name="executionThread">Instance of execution thread.</param>
    /// <returns>Result.</returns>
    public static string GetQueryResult(IExecutionThread<ExecutionOptions> executionThread)
    {
        var options = executionThread.Options;
        var stream = (MemoryStream)((DsvOutput)options.DefaultRowsOutput).Stream;
        stream.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd().Replace("\r\n", "\n").Trim();
    }

    /// <summary>
    /// Clear output result.
    /// </summary>
    /// <param name="executionThread">Instance of execution thread.</param>
    public static void ClearQueryResult(IExecutionThread<ExecutionOptions> executionThread)
    {
        var options = executionThread.Options;
        options.DefaultRowsOutput = CreateDsvOutput();
    }

    /// <summary>
    /// Get all test data files for XUnit tests.
    /// </summary>
    /// <returns>Enumerable of data.</returns>
    public static IEnumerable<object[]> GetTestFiles()
    {
        var rootDir = GetTestsDirectory();
        foreach (var testFile in Directory.EnumerateFiles(rootDir, "*.yaml", SearchOption.AllDirectories))
        {
            var relativeFilePath = Path.GetRelativePath(rootDir, testFile);
            yield return [relativeFilePath];
        }
    }

    public static IList<string> GetTestFilesList()
        => GetTestFiles().Select(f => f[0].ToString() ?? string.Empty).ToList();

    private static string GetDataDirectory()
        => Path.Combine(TestParser.FindTestsDirectory(), DataDirectory);

    private static string GetTestsDirectory()
        => Path.Combine(TestParser.FindTestsDirectory(), TestDirectory);
}
