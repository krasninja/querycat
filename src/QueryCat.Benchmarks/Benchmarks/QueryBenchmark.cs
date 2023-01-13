using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Execution;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class QueryBenchmark
{
    [Benchmark]
    public void QueryUsersCsvFile()
    {
        var executionThread = new ExecutionThread();
        new ExecutionThreadBootstrapper().Bootstrap(executionThread);
        var usersFile = UsersCsvFile.GetTestUsersFilePath();
        executionThread.Run(
            @$"select substr(email, position('@' in email)) as domain, avg(balance) into write_null() from '{usersFile}'" +
            " where lower(gender) = 'female' group by substr(email, position('@' in email)) order by domain fetch 10");
    }
}
