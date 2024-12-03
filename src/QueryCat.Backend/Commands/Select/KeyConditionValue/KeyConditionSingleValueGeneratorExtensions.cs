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
    /// <returns>Available values.</returns>
    public static VariantValue[] GetValues(this IKeyConditionSingleValueGenerator generator, IExecutionThread thread)
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
            while (multipleValuesGenerator.MoveNext(thread))
            {
                values.Add(multipleValuesGenerator.Get(thread));
            }
            multipleValuesGenerator.Reset();

            // Restore the original position.
            for (var position = 0; position < oldPosition + 1; position++)
            {
                multipleValuesGenerator.MoveNext(thread);
            }

            return values.ToArray();
        }

        return [generator.Get(thread)];
    }
}
