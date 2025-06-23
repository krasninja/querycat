using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using QueryCat.Backend;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Samples.Collection;

internal class InputClassUsage : BaseUsage
{
    internal class CountrySize
    {
        [Column("name")]
        [Description("Country name.")]
        public string Name { get; }

        [Column("size_in_sq_km")]
        [Description("Size.")]
        public decimal SizeInSqKm { get; }

        public CountrySize(string name, decimal sizeInSqKm)
        {
            Name = name;
            SizeInSqKm = sizeInSqKm;
        }
    }

    [Description("Country size rows input.")]
    [FunctionSignature("country_size_rows_input")]
    internal class CountrySizeRowsInput : EnumerableRowsInput<CountrySize>
    {
        /// <inheritdoc />
        protected override IEnumerable<CountrySize> GetData(Fetcher<CountrySize> fetcher)
        {
            yield return new CountrySize("Russia", 17_098_246);
            yield return new CountrySize("Canada", 9_984_670);
            yield return new CountrySize("China", 9_596_960);
            yield return new CountrySize("United States", 9_525_067);
            yield return new CountrySize("Brazil", 8_510_346);
        }
    }

    /// <inheritdoc />
    public override async Task RunAsync()
    {
        // Use "ExecutionThreadBootstrapper" class to create execution thread. It allows
        // run queries.
        await using var executionThread = new ExecutionThreadBootstrapper()
            .Create();

        executionThread.FunctionsManager.RegisterFunctions(
            executionThread.FunctionsManager.Factory.CreateFromType(typeof(CountrySizeRowsInput))
        );

        var result = await executionThread.RunAsync("country_size_rows_input()");
        Console.WriteLine(await SerializeValueToStringAsync(result));
    }
}
