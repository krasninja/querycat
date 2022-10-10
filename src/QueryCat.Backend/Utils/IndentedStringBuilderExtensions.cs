using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Extensions for <see cref="IndentedStringBuilder" />.
/// </summary>
public static class IndentedStringBuilderExtensions
{
    public static IndentedStringBuilder AppendRowsIterator(this IndentedStringBuilder stringBuilder,
        IRowsIterator rowsIterator)
    {
        rowsIterator.Explain(stringBuilder);
        return stringBuilder;
    }

    public static IndentedStringBuilder AppendRowsIteratorsWithIndent(
        this IndentedStringBuilder stringBuilder,
        string text,
        params IRowsIterator[] rowsIterators)
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

    public static IndentedStringBuilder AppendSubQueriesWithIndent(
        this IndentedStringBuilder stringBuilder,
        FuncUnit funcUnit)
    {
        stringBuilder.IncreaseIndent();
        foreach (var rowsIterator in funcUnit.SubQueryIterators)
        {
            rowsIterator.Explain(stringBuilder);
        }
        stringBuilder.DecreaseIndent();
        return stringBuilder;
    }

    public static IndentedStringBuilder AppendSubQueriesWithIndent(
        this IndentedStringBuilder stringBuilder,
        IEnumerable<FuncUnit> funcUnits)
    {
        foreach (var funcUnit in funcUnits)
        {
            AppendSubQueriesWithIndent(stringBuilder, funcUnit);
        }
        return stringBuilder;
    }
}
