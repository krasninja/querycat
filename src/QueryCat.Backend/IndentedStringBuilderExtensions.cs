using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend;

/// <summary>
/// Extensions for <see cref="IndentedStringBuilder" />.
/// </summary>
internal static class IndentedStringBuilderExtensions
{
    public static IndentedStringBuilder AppendRowsIteratorsWithIndent(
        this IndentedStringBuilder stringBuilder,
        string text,
        params ReadOnlySpan<IRowsIterator> rowsIterators)
    {
        stringBuilder.AppendLine(text);
        stringBuilder.IncreaseIndent();
        foreach (var rowsIterator in rowsIterators)
        {
            rowsIterator.Explain(stringBuilder);
        }
        stringBuilder.DecreaseIndent();
        return stringBuilder;
    }

    public static IndentedStringBuilder AppendRowsInputsWithIndent(
        this IndentedStringBuilder stringBuilder,
        string text,
        params ReadOnlySpan<IRowsInput> rowsInputs)
    {
        stringBuilder.AppendLine(text);
        stringBuilder.IncreaseIndent();
        foreach (var rowsInput in rowsInputs)
        {
            rowsInput.Explain(stringBuilder);
        }
        stringBuilder.DecreaseIndent();
        return stringBuilder;
    }
}
