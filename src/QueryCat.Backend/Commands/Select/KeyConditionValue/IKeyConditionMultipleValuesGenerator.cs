using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

/// <summary>
/// The values generation strategy that uses the same keys rows iterator to return multiple
/// results sets. For example, the query "SELECT * FROM source() WHERE key IN (1, 2, 3)" should
/// be executed 3 times with conditions "key = 1", "key = 2" and "key = 3".
/// </summary>
internal interface IKeyConditionMultipleValuesGenerator : IKeyConditionSingleValueGenerator
{
    /// <summary>
    /// Current cursor position. -1 is the initial.
    /// </summary>
    int Position { get; }

    /// <summary>
    /// Move to the next value.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Returns <c>True</c> if the value is available. Otherwise, <c>false</c>.</returns>
    ValueTask<bool> MoveNextAsync(IExecutionThread thread, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset the position to the initial element.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    ValueTask ResetAsync(CancellationToken cancellationToken = default);
}
