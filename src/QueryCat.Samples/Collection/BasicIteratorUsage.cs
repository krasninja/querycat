using QueryCat.Backend;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Samples.Collection;

internal class BasicIteratorUsage : BaseUsage
{
    /// <inheritdoc />
    public override async Task RunAsync()
    {
        await using var executionThread = new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .Create();
        var iterator = (await executionThread.RunAsync("select * from generate_series(1, 5);"))
            .AsRequired<IRowsIterator>();
        while (await iterator.MoveNextAsync())
        {
            Console.Write(iterator.Current[0].AsString + " "); // 1 2 3 4 5
        }
    }
}
