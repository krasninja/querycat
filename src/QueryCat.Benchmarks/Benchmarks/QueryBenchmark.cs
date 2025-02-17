using BenchmarkDotNet.Attributes;
using QueryCat.Backend;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class QueryBenchmark
{
    [Benchmark]
    public async Task QueryUsersCsvFile()
    {
        await using var executionThread = await new ExecutionThreadBootstrapper().CreateAsync();
        var usersFile = UsersCsvFile.GetTestUsersFilePath();
        await executionThread.RunAsync(
            @$"select substr(email, position('@' in email)) as domain, avg(balance) into write_null() from '{usersFile}'" +
            " where lower(gender) = 'female' group by substr(email, position('@' in email)) order by domain fetch 10");
    }
}
