using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionValueGeneratorEmpty : IKeyConditionMultipleValuesGenerator
{
    public static KeyConditionValueGeneratorEmpty Instance { get; } = new();

    /// <inheritdoc />
    public int Position => 0;

    /// <inheritdoc />
    public ValueTask<VariantValue?> GetAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new VariantValue?(VariantValue.Null));
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ResetAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
