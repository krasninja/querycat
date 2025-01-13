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
    /// <param name="value">Result value.</param>
    /// <returns><c>True</c> if able to return, <c>false</c> otherwise.</returns>
    bool TryGet(IExecutionThread thread, out VariantValue value);
}
