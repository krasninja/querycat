using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionValueGeneratorEmpty : IKeyConditionMultipleValuesGenerator
{
    public static KeyConditionValueGeneratorEmpty Instance { get; } = new();

    /// <inheritdoc />
    public int Position => 0;

    /// <inheritdoc />
    public VariantValue Get(IExecutionThread thread) => VariantValue.Null;

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public void Reset()
    {
    }
}
