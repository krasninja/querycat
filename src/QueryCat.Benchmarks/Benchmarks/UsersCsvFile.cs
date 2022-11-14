using QueryCat.Benchmarks.Commands;

namespace QueryCat.Benchmarks.Benchmarks;

internal static class UsersCsvFile
{
    public static string GetTestUsersFilePath()
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
                return file;
            }
        }

        throw new InvalidOperationException(
            $"Cannot find file {CreateTestCsvFileCommand.UsersFileName}, don't you forget to run 'create-test-csv' command?");
    }

    public static FileStream OpenTestUsersFile()
    {
        return File.OpenRead(GetTestUsersFilePath());
    }
}
