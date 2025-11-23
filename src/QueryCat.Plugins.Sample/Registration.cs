using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Plugins.Sample.Functions;
using QueryCat.Plugins.Sample.Inputs;

namespace QueryCat.Plugins.Sample;

/// <summary>
/// The special registration class that is called by plugin loader.
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Register plugin functions.
    /// </summary>
    /// <param name="functionsManager">Functions manager.</param>
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(AddressIterator.AddressIteratorFunction);
        functionsManager.RegisterFunction(AddressRowsInput.AddressRowsInputFunction);
        functionsManager.RegisterFunction(TestCombine.TestCombineFunction);
        functionsManager.RegisterFunction(TestSimpleNonStandard.TestSimpleNonStandardFunction);
        functionsManager.RegisterFunction(TestSimple.TestSimpleFunction);
        functionsManager.RegisterFunction(TestBlob.TestBlobFunction);
    }

    /// <summary>
    /// Load the plugin.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task OnLoadAsync(IExecutionThread executionThread, CancellationToken cancellationToken)
    {
        var result = await executionThread.RunAsync("select 'Hello, World!';", null, cancellationToken);
        var iterator = result.AsRequired<IRowsIterator>();
        while (await iterator.MoveNextAsync(cancellationToken))
        {
            Console.WriteLine(iterator.Current);
        }
    }
}
