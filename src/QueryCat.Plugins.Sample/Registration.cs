using QueryCat.Backend.Core.Functions;
using QueryCat.Plugins.Samples.Functions;
using QueryCat.Plugins.Samples.Inputs;

namespace QueryCat.Plugins.Samples;

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
}
