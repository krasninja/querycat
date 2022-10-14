using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Storage.Formats;
using QueryCat.Backend.Types;
using QueryCat.Benchmarks.Commands;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class CsvParseBenchmarks
{
    [Benchmark]
    public int ReadAllUsersWithDsvFormatter()
    {
        using var file = OpenTestUsersFile();
        var input = new DsvFormatter(',', addFileNameColumn: false).OpenInput(file);
        input.Open();
        var rowsFrame = new RowsFrame(input.Columns);
        var rowsIterator = input.AsIterable(autoFetch: true);
        rowsIterator.ToFrame(rowsFrame);
        return rowsFrame.TotalRows;
    }

    [Benchmark]
    public int ReadAllUsersWithStringSplit()
    {
        /*
         * The test is not the correct way of CSV parsing, it is just to test dumb implementation!
         */

        var rowsFrame = User.ClassBuilder.BuildRowsFrame();
        var row = new Row(rowsFrame);

        using var file = OpenTestUsersFile();
        using var streamReader = new StreamReader(file);
        streamReader.ReadLine(); // Read header.
        while (streamReader.ReadLine() is { } line)
        {
            var arr = line.Split(',');
            row[0] = new VariantValue(int.Parse(arr[0])); // Id.
            row[1] = new VariantValue(arr[1]); // Email.
            row[2] = new VariantValue(arr[2]); // FirstName.
            row[3] = new VariantValue(arr[3]); // LastName.
            row[4] = new VariantValue(null); // EmailVerifiedAt.
            row[5] = new VariantValue(arr[5]); // Address.
            row[6] = new VariantValue(arr[6]); // State.
            row[7] = new VariantValue(arr[7]); // Zip.
            row[8] = new VariantValue(arr[8]); // Phone.
            row[9] = new VariantValue(arr[9]); // Gender.
            row[10] = new VariantValue(DateTime.Parse(arr[10])); // DateOfBirth.
            row[11] = new VariantValue(Decimal.Parse(arr[11])); // Balance.
            row[12] = new VariantValue(DateTime.Parse(arr[12])); // CreatedAt.
            row[13] = new VariantValue(null); // RemovedAt.
            row[14] = new VariantValue(arr[14]); // Phrase.
            rowsFrame.AddRow(row);
        }

        return rowsFrame.TotalRows;
    }

    private FileStream OpenTestUsersFile()
    {
        string[] locations =
        {
            ".",
            "../../..",
            "../../../../../../..",
        };

        foreach (var location in locations)
        {
            var file = Path.Combine(location, CreateTestCsvFileCommand.UsersFileName);
            if (File.Exists(file))
            {
                return File.OpenRead(file);
            }
        }

        throw new InvalidOperationException(
            $"Cannot find file {CreateTestCsvFileCommand.UsersFileName}, don't you forget to run 'create-test-csv' command?");
    }
}
