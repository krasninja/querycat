using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend;

/// <summary>
/// The class is the delegate implementation for QueryCat project. It contains all the data
/// needed to run the node delegate and get the result.
/// </summary>
internal interface IFuncUnit
{
    /// <summary>
    /// The delegate output type.
    /// </summary>
    DataType OutputType { get; }

    /// <summary>
    /// Invoke and get value.
    /// </summary>
    /// <param name="thread">Current execution thread.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Value.</returns>
    ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default);
}
