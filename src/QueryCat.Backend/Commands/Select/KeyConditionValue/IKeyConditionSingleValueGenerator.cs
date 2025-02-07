using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

/// <summary>
/// The value generation strategy with only one value. It is used for
/// the equals condition like "x = 1".
/// </summary>
internal interface IKeyConditionSingleValueGenerator
{
    /// <summary>
    /// Get the current value.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current value or null if no value.</returns>
    ValueTask<VariantValue?> GetAsync(IExecutionThread thread, CancellationToken cancellationToken);
}
