using System.Diagnostics;
using System.Text;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            var name = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            Console.WriteLine($"Usage: {name} <qcat exe> <test directory>");
            return -1;
        }

        var qcatExecutable = args[0];
        var testDirectory = args[1];

        Directory.SetCurrentDirectory(testDirectory);
        Application.Culture = System.Globalization.CultureInfo.InvariantCulture;

        foreach (var file in TestThread.GetTestFilesList())
        {
            await Console.Out.WriteAsync($"Executing {file}... ");
            var data = TestThread.GetQueryData(file);
            using var process = new Process();

            process.StartInfo.FileName = qcatExecutable;
            SetupStartInfo(process.StartInfo, data.Query);
            var error = new StringBuilder();
            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                error.AppendLine(eventArgs.Data);
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode > 0)
            {
                await Console.Error.WriteLineAsync(error.ToString());
                return 1;
            }

            if (!await ProcessOutput(data.Expected, output))
            {
                return 1;
            }

            await Console.Out.WriteLineAsync("OK");
        }

        return 0;
    }

    private static void SetupStartInfo(ProcessStartInfo startInfo, string query)
    {
        startInfo.ArgumentList.Add("query");
        startInfo.ArgumentList.Add("--page-size=-1");
        startInfo.ArgumentList.Add("--no-header");
        startInfo.ArgumentList.Add("--float-format=F6");
        startInfo.ArgumentList.Add("--output-style=NoSpaceTable");
        startInfo.ArgumentList.Add(query);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
    }

    private static async Task<bool> ProcessOutput(string expected, string output)
    {
        var expectedInput = new DsvFormatter(',', hasHeader: false, addFileNameColumn: false).OpenInput(
            GetBlobFromString(expected));
        await expectedInput.OpenAsync();
        var expectedRowsFrame = await expectedInput.AsIterable(autoFetch: true).ToFrameAsync();
        await expectedInput.CloseAsync();

        output = output.Replace(VariantValue.NullValueString, string.Empty);
        var outputInput = new DsvFormatter('|', hasHeader: false, addFileNameColumn: false).OpenInput(
            GetBlobFromString(TrimFirstAndLastDelimiters(output, '|')));
        await outputInput.OpenAsync();
        var outputRowsFrame = await outputInput.AsIterable(autoFetch: true).ToFrameAsync();
        await outputInput.CloseAsync();

        if (expectedRowsFrame.TotalRows != outputRowsFrame.TotalRows)
        {
            await Console.Out.WriteLineAsync(
                $"ERROR\nExpected {expectedRowsFrame.TotalRows}, but got {outputRowsFrame.TotalRows}.");
            return false;
        }

        for (var i = 0; i < expectedRowsFrame.TotalRows; i++)
        {
            var expectedRow = expectedRowsFrame.GetRow(i);
            var outputRow = outputRowsFrame.GetRow(i);

            if (!expectedRow.Equals(outputRow))
            {
                await Console.Out.WriteLineAsync(
                    $"ERROR\n{expectedRow}\n{outputRow}");
                return false;
            }
        }

        return true;
    }

    private static MemoryStream GetStreamFromString(string target)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(target));
    }

    private static IBlobData GetBlobFromString(string target)
    {
        return new StreamBlobData(Encoding.UTF8.GetBytes(target));
    }

    private static string TrimFirstAndLastDelimiters(string target, char delimiter)
    {
        string TrimOne(string str, char separator)
        {
            if (str.Length > 0 && str[0] == separator)
            {
                str = str.Substring(1);
            }
            if (str.Length > 0 && str[^1] == separator)
            {
                str = str.Substring(0, str.Length - 1);
            }
            return str;
        }

        var sb = new StringBuilder();
        var arr = target
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => TrimOne(s, delimiter));
        foreach (var item in arr)
        {
            sb.AppendLine(item);
        }
        return sb.ToString();
    }
}
