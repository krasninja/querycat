using System.Globalization;
using BenchmarkDotNet.Attributes;
using CsvHelper;
using CsvHelper.Configuration;
using Sylvan.Data.Csv;
using CsvDataReader = Sylvan.Data.Csv.CsvDataReader;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class CsvParseBenchmarks
{
    [Benchmark]
    public int ReadAllUsersWithDsvFormatter()
    {
        using var file = UsersCsvFile.OpenTestUsersFile();
        var input = new DsvFormatter(',', addFileNameColumn: false).OpenInput(file);
        input.Open();
        var rowsFrame = new RowsFrame(input.Columns);
        var rowsIterator = input.AsIterable(autoFetch: true);
        rowsIterator.ToFrame(rowsFrame);
        return rowsFrame.TotalRows;
    }

    [Benchmark]
    public int ReadAllUsersWithDelimiterStreamReader()
    {
        var rowsFrame = User.ClassBuilder.BuildRowsFrame();
        var row = new Row(rowsFrame);

        using var file = UsersCsvFile.OpenTestUsersFile();
        var csv = new DelimiterStreamReader(new StreamReader(file), new DelimiterStreamReader.ReaderOptions
        {
            Delimiters = [','],
            QuoteChars = ['"'],
            Culture = CultureInfo.InvariantCulture,
        });

        csv.Read();
        while (csv.Read())
        {
            row[0] = new VariantValue(csv.GetInt32(0)); // Id.
            row[1] = new VariantValue(csv.GetField(1)); // Email.
            row[2] = new VariantValue(csv.GetField(2)); // FirstName.
            row[3] = new VariantValue(csv.GetField(3)); // LastName.
            var emailVerified = csv.GetField(4);
            row[4] = !emailVerified.IsEmpty ? new VariantValue(DateTime.Parse(emailVerified))
                : VariantValue.Null; // EmailVerifiedAt.
            row[5] = new VariantValue(csv.GetField(5)); // Address.
            row[6] = new VariantValue(csv.GetField(6)); // State.
            row[7] = new VariantValue(csv.GetField(7)); // Zip.
            row[8] = new VariantValue(csv.GetField(8)); // Phone.
            row[9] = new VariantValue(csv.GetField(9)); // Gender.
            row[10] = new VariantValue(csv.GetDateTime(10)); // DateOfBirth.
            row[11] = new VariantValue(csv.GetField(11)); // Balance.
            row[12] = new VariantValue(csv.GetField(12)); // CreatedAt.
            var removedAt = csv.GetField(13);
            row[13] = !removedAt.IsEmpty ? new VariantValue(DateTime.Parse(removedAt))
                : VariantValue.Null; // RemovedAt.
            row[14] = new VariantValue(csv.GetField(14)); // Phrase.
            rowsFrame.AddRow(row);
        }
        return rowsFrame.TotalRows;
    }

    [Benchmark]
    public int ReadAllUsersWithSylvanCsv()
    {
        var rowsFrame = User.ClassBuilder.BuildRowsFrame();
        var row = new Row(rowsFrame);

        using var file = UsersCsvFile.OpenTestUsersFile();
        using var csv = CsvDataReader.Create(new StreamReader(file), new CsvDataReaderOptions
        {
            Culture = CultureInfo.InvariantCulture,
            Schema = CsvSchema.Nullable
        });

        while (csv.Read())
        {
            row[0] = new VariantValue(csv.GetInt32(0)); // Id.
            row[1] = new VariantValue(csv.GetString(1)); // Email.
            row[2] = new VariantValue(csv.GetString(2)); // FirstName.
            row[3] = new VariantValue(csv.GetString(3)); // LastName.
            row[4] = !csv.IsDBNull(4) ? new VariantValue(csv.GetDateTime(4)) : VariantValue.Null; // EmailVerifiedAt.
            row[5] = new VariantValue(csv.GetString(5)); // Address.
            row[6] = new VariantValue(csv.GetString(6)); // State.
            row[7] = new VariantValue(csv.GetString(7)); // Zip.
            row[8] = new VariantValue(csv.GetString(8)); // Phone.
            row[9] = new VariantValue(csv.GetString(9)); // Gender.
            row[10] = new VariantValue(csv.GetDateTime(10)); // DateOfBirth.
            row[11] = new VariantValue(csv.GetDecimal(11)); // Balance.
            row[12] = new VariantValue(csv.GetDateTime(12)); // CreatedAt.
            row[13] = !csv.IsDBNull(13) ? new VariantValue(csv.GetDateTime(13)) : VariantValue.Null; // RemovedAt.
            row[14] = new VariantValue(csv.GetString(14)); // Phrase.
            rowsFrame.AddRow(row);
        }

        return rowsFrame.TotalRows;
    }

    [Benchmark]
    public int ReadAllUsersWithCsvHelper()
    {
        var rowsFrame = User.ClassBuilder.BuildRowsFrame();
        var row = new Row(rowsFrame);

        using var file = UsersCsvFile.OpenTestUsersFile();
        using var csv = new CsvReader(new StreamReader(file), new CsvConfiguration(CultureInfo.CurrentCulture));
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            row[0] = new VariantValue(csv.GetField<int>(0)); // Id.
            row[1] = new VariantValue(csv.GetField(1)); // Email.
            row[2] = new VariantValue(csv.GetField(2)); // FirstName.
            row[3] = new VariantValue(csv.GetField(3)); // LastName.
            var emailVerified = csv.GetField(4);
            row[4] = !string.IsNullOrEmpty(emailVerified) ? new VariantValue(DateTime.Parse(emailVerified))
                : VariantValue.Null; // EmailVerifiedAt.
            row[5] = new VariantValue(csv.GetField(5)); // Address.
            row[6] = new VariantValue(csv.GetField(6)); // State.
            row[7] = new VariantValue(csv.GetField(7)); // Zip.
            row[8] = new VariantValue(csv.GetField(8)); // Phone.
            row[9] = new VariantValue(csv.GetField(9)); // Gender.
            row[10] = new VariantValue(csv.GetField(10)); // DateOfBirth.
            row[11] = new VariantValue(csv.GetField<decimal>(11)); // Balance.
            row[12] = new VariantValue(csv.GetField<DateTime>(12)); // CreatedAt.
            var removedAt = csv.GetField(13);
            row[13] = !string.IsNullOrEmpty(removedAt) ? new VariantValue(DateTime.Parse(removedAt))
                : VariantValue.Null; // RemovedAt.
            row[14] = new VariantValue(csv.GetField(14)); // Phrase.
            rowsFrame.AddRow(row);
        }

        return rowsFrame.TotalRows;
    }

    [Benchmark]
    public int ReadAllUsersWithNRecoCsv()
    {
        var rowsFrame = User.ClassBuilder.BuildRowsFrame();
        var row = new Row(rowsFrame);

        using var file = UsersCsvFile.OpenTestUsersFile();
        var csv = new NReco.Csv.CsvReader(new StreamReader(file));
        csv.Read();

        while (csv.Read())
        {
            row[0] = new VariantValue(int.Parse(csv[0])); // Id.
            row[1] = new VariantValue(csv[1]); // Email.
            row[2] = new VariantValue(csv[2]); // FirstName.
            row[3] = new VariantValue(csv[3]); // LastName.
            var emailVerified = csv[4];
            row[4] = !string.IsNullOrEmpty(emailVerified) ? new VariantValue(DateTime.Parse(emailVerified))
                : VariantValue.Null; // EmailVerifiedAt.
            row[5] = new VariantValue(csv[5]); // Address.
            row[6] = new VariantValue(csv[6]); // State.
            row[7] = new VariantValue(csv[7]); // Zip.
            row[8] = new VariantValue(csv[8]); // Phone.
            row[9] = new VariantValue(csv[9]); // Gender.
            row[10] = new VariantValue(csv[10]); // DateOfBirth.
            row[11] = new VariantValue(decimal.Parse(csv[11])); // Balance.
            row[12] = new VariantValue(DateTime.Parse(csv[12])); // CreatedAt.
            var removedAt = csv[13];
            row[13] = !string.IsNullOrEmpty(removedAt) ? new VariantValue(DateTime.Parse(removedAt))
                : VariantValue.Null; // RemovedAt.
            row[14] = new VariantValue(csv[14]); // Phrase.
            rowsFrame.AddRow(row);
        }

        return rowsFrame.TotalRows;
    }

    [Benchmark]
    public int ReadAllUsersWithNaiveStringSplit()
    {
        /*
         * The test is not the correct way of CSV parsing, it is just to test dumb implementation!
         */

        var rowsFrame = User.ClassBuilder.BuildRowsFrame();
        var row = new Row(rowsFrame);

        using var file = UsersCsvFile.OpenTestUsersFile();
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
            row[11] = new VariantValue(decimal.Parse(arr[11])); // Balance.
            row[12] = new VariantValue(DateTime.Parse(arr[12])); // CreatedAt.
            row[13] = new VariantValue(null); // RemovedAt.
            row[14] = new VariantValue(arr[14]); // Phrase.
            rowsFrame.AddRow(row);
        }

        return rowsFrame.TotalRows;
    }
}
