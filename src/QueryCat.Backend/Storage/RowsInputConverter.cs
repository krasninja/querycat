using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Utilities for <see cref="IRowsInput" />.
/// </summary>
internal static class RowsInputConverter
{
    private static int NextInputIndex { get; set; }

    public static KeyValuePair<string, IRowsInput?> Convert(VariantValue source)
    {
        if (source.Type == DataType.Object || source.Type == DataType.Dynamic)
        {
            // If we have input or iterator - add it to sources list.
            if (source.AsObjectUnsafe is IRowsInput rowsInput)
            {
                return new KeyValuePair<string, IRowsInput?>(GetNextInputName(), rowsInput);
            }
            if (source.AsObjectUnsafe is IRowsIterator rowsIterator)
            {
                return new KeyValuePair<string, IRowsInput?>(GetNextInputName(), new RowsIteratorInput(rowsIterator));
            }
        }
        return new KeyValuePair<string, IRowsInput?>(string.Empty, null);
    }

    public static async Task<KeyValuePair<string, IRowsInput?>> ResolveInputAsync(
        IExecutionThread thread,
        string source,
        CancellationToken cancellationToken = default)
    {
        IRowsInput? ConvertToInput(VariantValue value)
        {
            if (value.Type != DataType.Object && value.Type != DataType.Dynamic)
            {
                return null;
            }

            if (value.AsObjectUnsafe is IRowsInput rowsInput)
            {
                return rowsInput;
            }
            if (value.AsObjectUnsafe is IRowsIterator rowsIterator)
            {
                return new RowsIteratorInput(rowsIterator);
            }
            return null;
        }

        var split = StringUtils.GetFieldsFromLine(source, delimiter: '=');
        var name = split.Length == 2 ? split[0] : GetNextInputName();
        var command = string.Join(' ', Application.CommandOpen, StringUtils.Quote(split[^1], quote: "'", force: true));
        var rowsInputValue = await thread.RunAsync(command, cancellationToken: cancellationToken);
        var rowsInput = ConvertToInput(rowsInputValue);
        return new KeyValuePair<string, IRowsInput?>(name, rowsInput);
    }

    private static string GetNextInputName() => "input" + NextInputIndex++;
}
