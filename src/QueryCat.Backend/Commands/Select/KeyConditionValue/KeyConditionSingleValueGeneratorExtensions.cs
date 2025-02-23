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
                await multipleValuesGenerator.ResetAsync(cancellationToken);
            }
            while (await multipleValuesGenerator.MoveNextAsync(thread, cancellationToken))
            {
                var nullableValue = await multipleValuesGenerator.GetAsync(thread, cancellationToken);
                if (nullableValue.HasValue)
                {
                    values.Add(nullableValue.Value);
                }
            }
            await multipleValuesGenerator.ResetAsync(cancellationToken);

            // Restore the original position.
            for (var position = 0; position < oldPosition + 1; position++)
            {
                await multipleValuesGenerator.MoveNextAsync(thread, cancellationToken);
            }

            return values.ToArray();
        }

        var generatorNullableValue = await generator.GetAsync(thread, cancellationToken);
        return generatorNullableValue.HasValue ? [generatorNullableValue.Value] : [];
    }
}
