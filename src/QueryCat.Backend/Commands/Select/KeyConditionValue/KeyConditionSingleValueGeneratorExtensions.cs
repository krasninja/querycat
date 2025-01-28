using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

/// <summary>
/// Extensions for <see cref="IKeyConditionSingleValueGenerator" />.
/// </summary>
internal static class KeyConditionSingleValueGeneratorExtensions
{
    /// <summary>
    /// Get all available values for the specified generator.
    /// </summary>
    /// <param name="generator">Values generator.</param>
    /// <param name="thread">Execution thread.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Available values.</returns>
    public static async ValueTask<VariantValue[]> GetValues(
        this IKeyConditionSingleValueGenerator generator,
        IExecutionThread thread,
        CancellationToken cancellationToken = default)
    {
        if (generator is IKeyConditionMultipleValuesGenerator multipleValuesGenerator)
        {
            // Keep the original position.
            var oldPosition = multipleValuesGenerator.Position;
            var values = new List<VariantValue>();
            if (oldPosition > -1)
            {
                multipleValuesGenerator.Reset();
            }
            while (await multipleValuesGenerator.MoveNextAsync(thread, cancellationToken))
            {
                if (multipleValuesGenerator.TryGet(thread, out var value))
                {
                    values.Add(value);
                }
            }
            multipleValuesGenerator.Reset();

            // Restore the original position.
            for (var position = 0; position < oldPosition + 1; position++)
            {
                await multipleValuesGenerator.MoveNextAsync(thread, cancellationToken);
            }

            return values.ToArray();
        }

        return generator.TryGet(thread, out var variantValue) ? [variantValue] : [];
    }
}
