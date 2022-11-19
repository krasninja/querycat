using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

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

    public static IndentedStringBuilder AppendRowsInputsWithIndent(
        this IndentedStringBuilder stringBuilder,
        string text,
        params IRowsInput[] rowsInputs)
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

    public static IndentedStringBuilder AppendSubQueriesWithIndent(
        this IndentedStringBuilder stringBuilder,
        IFuncUnit funcUnit)
    {
        if (funcUnit.GetData(FuncUnit.SubqueriesRowsIterators) is IEnumerable<IRowsIterator> subqueriesRowsIterators)
        {
            stringBuilder.IncreaseIndent();
            foreach (var rowsIterator in subqueriesRowsIterators)
            {
                rowsIterator.Explain(stringBuilder);
            }
            stringBuilder.DecreaseIndent();
        }

        return stringBuilder;
    }

    public static IndentedStringBuilder AppendSubQueriesWithIndent(
        this IndentedStringBuilder stringBuilder,
        IEnumerable<IFuncUnit> funcUnits)
    {
        foreach (var funcUnit in funcUnits)
        {
            AppendSubQueriesWithIndent(stringBuilder, funcUnit);
        }
        return stringBuilder;
    }
}
